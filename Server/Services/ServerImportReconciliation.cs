using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using GIBS.Module.FBClients.Fulfillment;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Oqtane.Enums;
using Oqtane.Shared;

namespace GIBS.Module.FBClients.ImportData
{
    public partial class ServerImportDataService
    {
        public async Task<ImportReconciliationReport> ReconcileAsync(int moduleId, ImportPreflightRequest request)
        {
            var (tenant, site) = await AuthorizeAsync(moduleId);
            var destination = request == null || request.DestinationModuleId <= 0 ? null : modules.GetModule(request.DestinationModuleId);
            if (destination == null || destination.IsDeleted || destination.SiteId != site || destination.ModuleDefinitionName != FulfillmentModule.ClientsDefinitionName)
                throw new ValidationException("Select an FBClients destination module on this site.");
            var plan = (await GetConfigurationAsync(moduleId)).Plan;
            if (plan == null || string.IsNullOrWhiteSpace(plan.Source) || plan.SourceModuleId is null or <= 0
                || plan.Tables == null || plan.Tables.Count == 0 || plan.Tables.Distinct(StringComparer.Ordinal).Count() != plan.Tables.Count
                || plan.Tables.Any(name => !Mappings.Any(map => map.Source == name)))
                throw new ValidationException("Save a valid source, source ModuleID, and table selection before reconciliation.");
            if (plan.Tables.Any(name => name.EndsWith("_Merged", StringComparison.Ordinal)))
                throw new ValidationException("Merged-table reconciliation is not implemented. Omit merged tables from the selection.");
            if (request.IncludeTurkeyRecipients && !plan.Tables.Contains("GIBS_FBTurkeyRecipients", StringComparer.Ordinal))
                plan = plan with { Tables = plan.Tables.Append("GIBS_FBTurkeyRecipients").ToList() };
            var selected = Mappings.Where(map => plan.Tables.Contains(map.Source, StringComparer.Ordinal)).ToList();
            var connectionString = ResolveSourceConnection(tenant, site, plan.Source, moduleId);
            var operation = "opening source database";
            try
            {
                var builder = new SqlConnectionStringBuilder(connectionString) { ApplicationName = "FBClients Import Reconciliation", ApplicationIntent = ApplicationIntent.ReadOnly };
                if (string.IsNullOrWhiteSpace(builder.InitialCatalog)) throw new ValidationException("The DNN source must specify a database.");
                await using var source = new SqlConnection(builder.ConnectionString);
                await source.OpenAsync();
                await using var db = factory.CreateDbContext();
                if (!db.Database.IsSqlServer()) throw new ValidationException("Reconciliation currently supports SQL Server destinations only.");
                await db.Database.OpenConnectionAsync();
                var target = db.Database.GetDbConnection();
                var sourceClients = await ColumnsAsync(source, "GIBS_FBClients");
                var targetClients = await ColumnsAsync(target, "GIBSFBClients");
                if (!HasInt(sourceClients, "ClientID") || !HasInt(sourceClients, "ModuleID")
                    || !HasInt(targetClients, "FBClientsId") || !HasInt(targetClients, "ModuleId")
                    || !await IsIdentityPrimaryKeyAsync(target, "GIBSFBClients", "FBClientsId"))
                    throw new ValidationException("Source or destination client scope metadata is missing or invalid; reconciliation cannot run.");
                if (await ScalarAsync(source, "SELECT COUNT_BIG(*) FROM [dbo].[GIBS_FBClients] WHERE ModuleID=@sourceModuleId", plan.SourceModuleId.Value) == 0)
                    throw new ValidationException("The selected DNN ModuleID has no source clients.");
                var results = new List<ImportReconciliationTable>();
                foreach (var map in selected)
                {
                    operation = $"reconciling {map.Source}";
                    var sourceColumns = await ColumnsAsync(source, map.Source);
                    var targetColumns = await ColumnsAsync(target, map.Destination);
                    if (!HasInt(sourceColumns, map.SourceKey) || !HasInt(targetColumns, map.DestinationKey)
                        || !await IsIdentityPrimaryKeyAsync(target, map.Destination, map.DestinationKey)
                        || (map.Parent != null && (!HasInt(sourceColumns, map.Parent) || !HasInt(targetColumns, "FBClientsId"))))
                        throw new ValidationException($"Required key or parent metadata for {map.Source} is missing or invalid; reconciliation cannot run.");
                    if (map.Source == "GIBS_FBxMas")
                    {
                        var family = await ColumnsAsync(source, "GIBS_FBClientsAddFamMem");
                        var targetFamily = await ColumnsAsync(target, "GIBSFBClientsAddFamMem");
                        if (!HasInt(sourceColumns, "ClAddFamMemID") || !HasInt(family, "ClAddFamMemID") || !HasInt(family, "ClientID")
                            || !HasInt(targetColumns, "FBClientsAddFamMemId") || !HasInt(targetColumns, "LegacyClAddFamMemId")
                            || !HasInt(targetFamily, "FBClientsAddFamMemId") || !HasInt(targetFamily, "FBClientsId"))
                            throw new ValidationException("Christmas family metadata is missing or invalid; reconciliation cannot run.");
                    }
                    var integrity = await CheckDestinationIntegrityAsync(target, map.Destination);
                    if (integrity.UnsafeConstraints > 0 || integrity.DisabledIndexes > 0 || integrity.DisabledTriggers > 0)
                        throw new ValidationException($"Destination {map.Destination} has disabled or untrusted constraints, disabled unique indexes, or disabled triggers; reconciliation cannot verify a safe import target.");
                    var valueMismatches = await CompareMappedValuesAsync(source, target, map, plan.SourceModuleId.Value, destination.ModuleId);
                    var result = await CompareReconciliationAsync(source, target, map, plan.SourceModuleId.Value, destination.ModuleId);
                    result = result with { MappedValueMismatchRows = valueMismatches };
                    if (map.Parent != null)
                        result = result with { SourceWideOrphans = await MissingParentsAsync(source, map.Source, map.Parent, map.ParentTable, map.ParentKey) };
                    results.Add(result);
                }
                return new(plan.Source, plan.SourceModuleId.Value, destination.ModuleId, results);
            }
            catch (SqlException ex)
            {
                logger.Log(LogLevel.Error, this, LogFunction.Read,
                    "Import reconciliation failed for module {ModuleId} while {Operation}: SQL error {SqlNumber}, state {SqlState}, procedure {Procedure}, line {LineNumber}.",
                    moduleId, operation, ex.Number, ex.State, ex.Procedure ?? "", ex.LineNumber);
                throw new ImportDataException("Reconciliation could not read source or destination data. Check the server logs for the SQL error number and location, then verify database permissions, connectivity, and schema.");
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
            {
                logger.Log(LogLevel.Error, this, LogFunction.Read, "Import reconciliation failed for module {ModuleId} while {Operation}: {ErrorType}", moduleId, operation, ex.GetType().Name);
                throw new ImportDataException("Reconciliation could not read source or destination data. Check database permissions, connectivity, timeouts, and server logs.");
            }
        }

        private static async Task<ImportReconciliationTable> CompareReconciliationAsync(DbConnection source, DbConnection target, TableMap map, int sourceModuleId, int destinationModuleId)
        {
            var christmas = map.Source == "GIBS_FBxMas";
            var sourceParent = map.Parent == null ? "CAST(NULL AS int)" : "x." + Quote(map.Parent);
            var expectedFamily = christmas
                ? "CASE WHEN EXISTS (SELECT 1 FROM [dbo].[GIBS_FBClientsAddFamMem] f WHERE f.ClAddFamMemID=x.ClAddFamMemID AND f.ClientID=x.ClientID) THEN x.ClAddFamMemID ELSE NULL END"
                : "CAST(NULL AS int)";
            var conflictingFamily = christmas
                ? "CASE WHEN NOT EXISTS (SELECT 1 FROM [dbo].[GIBS_FBClientsAddFamMem] f WHERE f.ClAddFamMemID=x.ClAddFamMemID AND f.ClientID=x.ClientID) AND EXISTS (SELECT 1 FROM [dbo].[GIBS_FBClientsAddFamMem] f WHERE f.ClAddFamMemID=x.ClAddFamMemID) THEN 1 ELSE 0 END"
                : "0";
            var sourceLegacyFamily = christmas ? "x.ClAddFamMemID" : "CAST(NULL AS int)";
            await using var sourceCommand = source.CreateCommand();
            sourceCommand.CommandTimeout = 120;
            sourceCommand.CommandText = $"SELECT x.{Quote(map.SourceKey)}, {sourceParent}, {expectedFamily}, {conflictingFamily}, {sourceLegacyFamily} FROM [dbo].{Quote(map.Source)} x WHERE {SourceScope(map)} ORDER BY x.{Quote(map.SourceKey)}";
            AddSourceModuleParameter(sourceCommand, sourceModuleId);
            await using var targetCommand = target.CreateCommand();
            targetCommand.CommandTimeout = 120;
            var targetParent = map.Parent == null ? "CAST(NULL AS int)" : "x.[FBClientsId]";
            var targetFamily = christmas ? "x.[FBClientsAddFamMemId]" : "CAST(NULL AS int)";
            var targetLegacyFamily = christmas ? "x.[LegacyClAddFamMemId]" : "CAST(NULL AS int)";
            var familyExists = christmas ? "CASE WHEN x.FBClientsAddFamMemId IS NULL OR EXISTS (SELECT 1 FROM [dbo].[GIBSFBClientsAddFamMem] f WHERE f.FBClientsAddFamMemId=x.FBClientsAddFamMemId AND f.FBClientsId=x.FBClientsId) THEN 1 ELSE 0 END" : "1";
            var join = map.Parent == null ? "" : " LEFT JOIN [dbo].[GIBSFBClients] p ON p.FBClientsId=x.FBClientsId";
            var owner = map.Parent == null ? "x.ModuleId" : "p.ModuleId";
            targetCommand.CommandText = $"SELECT x.{Quote(map.DestinationKey)}, {targetParent}, {targetFamily}, {owner}, {targetLegacyFamily}, {familyExists} FROM [dbo].{Quote(map.Destination)} x{join} ORDER BY x.{Quote(map.DestinationKey)}";
            await using var sourceReader = await sourceCommand.ExecuteReaderAsync();
            await using var targetReader = await targetCommand.ExecuteReaderAsync();
            var hasTarget = await targetReader.ReadAsync();
            long sourceRows = 0, targetRows = 0, matched = 0, missing = 0, extra = 0, wrongOwner = 0, wrongParent = 0, wrongFamily = 0, conflicts = 0, unowned = 0;
            int? previous = null;
            void CountTarget()
            {
                if (targetReader.IsDBNull(3)) unowned++;
                else if (targetReader.GetInt32(3) == destinationModuleId) targetRows++;
            }
            if (hasTarget) CountTarget();
            async Task AdvanceTarget()
            {
                hasTarget = await targetReader.ReadAsync();
                if (hasTarget) CountTarget();
            }
            while (await sourceReader.ReadAsync())
            {
                sourceRows++;
                if (sourceReader.IsDBNull(0)) throw new ValidationException($"{map.Source} has a null source key; reconciliation cannot continue.");
                var id = sourceReader.GetInt32(0);
                if (id <= 0 || previous == id) throw new ValidationException($"{map.Source} has a nonpositive or duplicate source key; reconciliation cannot continue.");
                previous = id;
                if (sourceReader.GetInt32(3) != 0) conflicts++;
                while (hasTarget && targetReader.GetInt32(0) < id)
                {
                    if (!targetReader.IsDBNull(3) && targetReader.GetInt32(3) == destinationModuleId) extra++;
                    await AdvanceTarget();
                }
                if (!hasTarget || targetReader.GetInt32(0) != id) { missing++; continue; }
                if (targetReader.IsDBNull(3) || targetReader.GetInt32(3) != destinationModuleId) wrongOwner++;
                else
                {
                    matched++;
                    if (map.Parent != null && (sourceReader.IsDBNull(1) || targetReader.IsDBNull(1) || sourceReader.GetInt32(1) != targetReader.GetInt32(1))) wrongParent++;
                    if (christmas && (NullableInt(sourceReader, 2) != NullableInt(targetReader, 2)
                        || NullableInt(sourceReader, 4) != NullableInt(targetReader, 4) || targetReader.GetInt32(5) == 0)) wrongFamily++;
                }
                await AdvanceTarget();
            }
            while (hasTarget)
            {
                if (!targetReader.IsDBNull(3) && targetReader.GetInt32(3) == destinationModuleId) extra++;
                await AdvanceTarget();
            }
            return new(map.Source, map.Destination, sourceRows, targetRows, matched, missing, extra, wrongOwner, wrongParent, wrongFamily, conflicts, unowned, null);
        }

        private static async Task<long> CompareMappedValuesAsync(DbConnection source, DbConnection target, TableMap map, int sourceModuleId, int destinationModuleId)
        {
            var fields = FieldMappings(map);
            var sourceColumns = await ColumnShapesAsync(source, map.Source);
            var targetColumns = await ColumnShapesAsync(target, map.Destination);
            var comparable = new List<FieldMap>();
            var incomeAbsent = map.Source == "GIBS_FBClients" && !sourceColumns.ContainsKey("PrimaryIncomeSource")
                && await IncomeColumnAbsentAsync(source);
            foreach (var field in fields)
            {
                if (!targetColumns.ContainsKey(field.Destination))
                    throw new ValidationException($"Mapped destination column {field.Destination} is missing; value reconciliation cannot run.");
                if (field.Destination is "CreatedBy" or "ModifiedBy" or "CreatedOn" or "ModifiedOn") continue;
                if (field.Source == null && field.Destination != "ModuleId") continue;
                if (field.Source != null && !sourceColumns.ContainsKey(field.Source)
                    && !(incomeAbsent && field.Destination == "PrimaryIncomeSource"))
                    throw new ValidationException($"Mapped source column {field.Source} is missing or not visible; value reconciliation cannot run.");
                comparable.Add(field);
            }
            var expected = comparable.Select(field =>
            {
                string value;
                if (field.Destination == "ModuleId") value = "@destinationModuleId";
                else if (map.Source == "GIBS_FBxMas" && field.Destination == "FBClientsAddFamMemId")
                    value = "CASE WHEN EXISTS (SELECT 1 FROM [dbo].[GIBS_FBClientsAddFamMem] f WHERE f.ClAddFamMemID=x.ClAddFamMemID AND f.ClientID=x.ClientID) THEN x.ClAddFamMemID ELSE NULL END";
                else if (incomeAbsent && field.Destination == "PrimaryIncomeSource") value = "CONVERT(nvarchar(50),N'')";
                else value = "x." + Quote(field.Source);
                if (targetColumns[field.Destination].Type == "date") value = $"CONVERT(date,{value})";
                return value;
            }).ToArray();
            var parentJoin = map.Parent == null ? "" : " LEFT JOIN [dbo].[GIBSFBClients] p ON p.FBClientsId=x.FBClientsId";
            var owner = map.Parent == null ? "x.ModuleId" : "p.ModuleId";
            await using var sourceCommand = source.CreateCommand();
            sourceCommand.CommandTimeout = 120;
            sourceCommand.CommandText = $"SELECT x.{Quote(map.SourceKey)}, {string.Join(",", expected)} FROM [dbo].{Quote(map.Source)} x WHERE {SourceScope(map)} ORDER BY x.{Quote(map.SourceKey)}";
            AddSourceModuleParameter(sourceCommand, sourceModuleId);
            var destinationParameter = sourceCommand.CreateParameter();
            destinationParameter.ParameterName = "@destinationModuleId";
            destinationParameter.DbType = System.Data.DbType.Int32;
            destinationParameter.Value = destinationModuleId;
            sourceCommand.Parameters.Add(destinationParameter);
            await using var targetCommand = target.CreateCommand();
            targetCommand.CommandTimeout = 120;
            var mappedColumns = string.Join(",", comparable.Select(field => "x." + Quote(field.Destination)));
            var auditColumns = "x.[CreatedBy],x.[ModifiedBy],x.[CreatedOn],x.[ModifiedOn]";
            targetCommand.CommandText = $"SELECT x.{Quote(map.DestinationKey)}, {owner}, {mappedColumns}, {auditColumns} FROM [dbo].{Quote(map.Destination)} x{parentJoin} ORDER BY x.{Quote(map.DestinationKey)}";
            await using var sourceReader = await sourceCommand.ExecuteReaderAsync();
            await using var targetReader = await targetCommand.ExecuteReaderAsync();
            var hasTarget = await targetReader.ReadAsync();
            long mismatches = 0;
            while (await sourceReader.ReadAsync())
            {
                var id = sourceReader.GetInt32(0);
                while (hasTarget && targetReader.GetInt32(0) < id) hasTarget = await targetReader.ReadAsync();
                if (!hasTarget || targetReader.GetInt32(0) != id) continue;
                if (!targetReader.IsDBNull(1) && targetReader.GetInt32(1) == destinationModuleId)
                {
                    var rowMismatch = false;
                    for (var i = 0; i < comparable.Count; i++)
                        if (!MappedValuesEqual(sourceReader.GetValue(i + 1), targetReader.GetValue(i + 2), targetColumns[comparable[i].Destination]))
                            rowMismatch = true;
                    var auditOffset = comparable.Count + 2;
                    if (targetReader.IsDBNull(auditOffset) || targetReader.IsDBNull(auditOffset + 1)
                        || !string.Equals(Convert.ToString(targetReader.GetValue(auditOffset), System.Globalization.CultureInfo.InvariantCulture),
                            Convert.ToString(targetReader.GetValue(auditOffset + 1), System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal)
                        || targetReader.IsDBNull(auditOffset + 2) || targetReader.IsDBNull(auditOffset + 3)
                        || !Equals(targetReader.GetValue(auditOffset + 2), targetReader.GetValue(auditOffset + 3)))
                        rowMismatch = true;
                    if (rowMismatch) mismatches++;
                }
                hasTarget = await targetReader.ReadAsync();
            }
            return mismatches;
        }

        private static bool MappedValuesEqual(object expected, object actual, ColumnShape target)
        {
            if (expected is DBNull) expected = null;
            if (actual is DBNull) actual = null;
            if (expected == null || actual == null) return expected == actual;
            return target.Type switch
            {
                "nvarchar" or "varchar" or "nchar" or "char" => string.Equals(Convert.ToString(expected, System.Globalization.CultureInfo.InvariantCulture), Convert.ToString(actual, System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal),
                "int" => Convert.ToInt32(expected, System.Globalization.CultureInfo.InvariantCulture) == Convert.ToInt32(actual, System.Globalization.CultureInfo.InvariantCulture),
                "bit" => Convert.ToBoolean(expected, System.Globalization.CultureInfo.InvariantCulture) == Convert.ToBoolean(actual, System.Globalization.CultureInfo.InvariantCulture),
                "decimal" or "numeric" => Convert.ToDecimal(expected, System.Globalization.CultureInfo.InvariantCulture) == Convert.ToDecimal(actual, System.Globalization.CultureInfo.InvariantCulture),
                "date" => Convert.ToDateTime(expected, System.Globalization.CultureInfo.InvariantCulture).Date == Convert.ToDateTime(actual, System.Globalization.CultureInfo.InvariantCulture).Date,
                "datetime" or "datetime2" => Convert.ToDateTime(expected, System.Globalization.CultureInfo.InvariantCulture) == Convert.ToDateTime(actual, System.Globalization.CultureInfo.InvariantCulture),
                "varbinary" or "binary" => expected is byte[] expectedBytes && actual is byte[] actualBytes && expectedBytes.AsSpan().SequenceEqual(actualBytes),
                _ => Equals(expected, actual)
            };
        }

        private static int? NullableInt(DbDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);
    }
}
