using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Common;
using System.Linq;
using System.Text.Json;
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
        private record TableMap(string Source, string Destination, string SourceKey, string DestinationKey, string Parent = null, string ParentTable = null, string ParentKey = null);
        private static readonly TableMap[] Mappings =
        [
            new("GIBS_FBClients", "GIBSFBClients", "ClientID", "FBClientsId"),
            new("GIBS_FBClientsAddFamMem", "GIBSFBClientsAddFamMem", "ClAddFamMemID", "FBClientsAddFamMemId", "ClientID", "GIBS_FBClients", "ClientID"),
            new("GIBS_FBClientsVisits", "GIBSFBClientsVisits", "VisitID", "VisitId", "ClientID", "GIBS_FBClients", "ClientID"),
            new("GIBS_FBClientsIncomeAndExpense", "GIBSFBClientsIncomeAndExpense", "IeID", "IeId", "ClientID", "GIBS_FBClients", "ClientID"),
            new("GIBS_FBClientsTrueFalseQuestions", "GIBSFBClientsTrueFalseQuestions", "TfID", "TfId", "ClientID", "GIBS_FBClients", "ClientID"),
            new("GIBS_FBClientsIDPhoto", "GIBSFBClientsIDPhoto", "ImageID", "ImageId", "ClientID", "GIBS_FBClients", "ClientID"),
            new("GIBS_FBxMas", "GIBSFBClientsChristmas", "xMasID", "xMasId", "ClientID", "GIBS_FBClients", "ClientID"),
            new("GIBS_FBTurkeyRecipients", "GIBSFBTurkeyRecipients", "TurkeyID", "TurkeyId", "ClientID", "GIBS_FBClients", "ClientID"),
            new("GIBS_FBClients_Merged", "GIBSFBClientsMerged", "ClientID", "LegacyRecordId"),
            new("GIBS_FBClientsAddFamMem_Merged", "GIBSFBClientsAddFamMemMerged", "ClAddFamMemID", "LegacyRecordId"),
            new("GIBS_FBClientsVisits_Merged", "GIBSFBClientsVisitsMerged", "VisitID", "LegacyRecordId"),
            new("GIBS_FBClientsIncomeAndExpense_Merged", "GIBSFBClientsIncomeAndExpenseMerged", "IeID", "LegacyRecordId"),
            new("GIBS_FBClientsTrueFalseQuestions_Merged", "GIBSFBClientsTrueFalseQuestionsMerged", "TfID", "LegacyRecordId"),
            new("GIBS_FBxMas_Merged", "GIBSFBClientsChristmasMerged", "xMasID", "LegacyRecordId")
        ];

        public async Task<ImportPreflightReport> PreflightAsync(int moduleId, ImportPreflightRequest request)
        {
            var (tenant, site) = await AuthorizeAsync(moduleId);
            var destination = request == null || request.DestinationModuleId <= 0 ? null : modules.GetModule(request.DestinationModuleId);
            if (destination == null || destination.IsDeleted || destination.SiteId != site || destination.ModuleDefinitionName != FulfillmentModule.ClientsDefinitionName)
                throw new ValidationException("Select an FBClients destination module on this site.");
            var stored = settings.GetSetting(EntityNames.Module, moduleId, ImportDataModule.PlanSetting)?.SettingValue;
            if (string.IsNullOrEmpty(stored)) throw new ValidationException("Save a source and table selection before running preflight.");
            ImportDataPlan plan;
            try { plan = JsonSerializer.Deserialize<ImportDataPlan>(stored); }
            catch (JsonException) { throw new ValidationException("The saved selection is invalid. Save the table selection again."); }
            if (plan == null || string.IsNullOrWhiteSpace(plan.Source) || plan.Tables == null || plan.Tables.Count == 0
                || plan.Tables.Distinct(StringComparer.Ordinal).Count() != plan.Tables.Count
                || plan.Tables.Any(name => !Mappings.Any(x => x.Source == name)))
                throw new ValidationException("The saved selection is invalid. Save the table selection again.");
            if (plan.SourceModuleId is null or <= 0)
                throw new ValidationException("Enter and save the DNN source ModuleID before running preflight.");
            if (request.IncludeTurkeyRecipients && !plan.Tables.Contains("GIBS_FBTurkeyRecipients", StringComparer.Ordinal))
                plan = plan with { Tables = plan.Tables.Append("GIBS_FBTurkeyRecipients").ToList() };
            var sourceModuleId = plan.SourceModuleId.Value;

            var issues = new List<ImportPreflightIssue>
            {
                new("Blocker", "", "This report is read-only and import remains disabled. The batch writer has only been rehearsed with synthetic data; the actual-source rehearsal and operational approval controls are not complete. Do not use this report as approval to import."),
                new("Warning", "", $"Source counts and ID collisions are restricted to clients with DNN ModuleID {sourceModuleId} and their history. Other modules are excluded. Orphan history is reported separately because its module cannot be determined. Use a consistent source snapshot and repeat preflight before cutover."),
                new("Warning", "", "Visit order items and inventory are excluded. Imported visit statuses may describe orders whose items have not been imported.")
            };
            var results = new List<ImportPreflightTable>();
            var selected = Mappings.Where(x => plan.Tables.Contains(x.Source, StringComparer.Ordinal)).ToList();
            foreach (var map in selected.Where(x => x.Parent != null && !plan.Tables.Contains(x.ParentTable, StringComparer.Ordinal)))
                issues.Add(new("Blocker", map.Source, $"Select its parent table {map.ParentTable}; the destination parent mapping is not yet implemented."));
            if (plan.Tables.Contains("GIBS_FBxMas", StringComparer.Ordinal) && !plan.Tables.Contains("GIBS_FBClientsAddFamMem", StringComparer.Ordinal))
                issues.Add(new("Blocker", "GIBS_FBxMas", "Select family members for the Christmas family relationship."));

            var sourceConnection = ResolveSourceConnection(tenant, site, plan.Source, moduleId);
            try
            {
                var builder = new SqlConnectionStringBuilder(sourceConnection) { ApplicationName = "FBClients Import Preflight", ApplicationIntent = ApplicationIntent.ReadOnly };
                if (string.IsNullOrWhiteSpace(builder.InitialCatalog)) throw new ValidationException("The DNN source must specify a database.");
                await using var source = new SqlConnection(builder.ConnectionString);
                await source.OpenAsync();
                var scopeColumns = await ColumnsAsync(source, "GIBS_FBClients");
                if (!HasInt(scopeColumns, "ClientID") || !HasInt(scopeColumns, "ModuleID"))
                    throw new ValidationException("Source client ID or ModuleID metadata is missing or not visible; scoped preflight cannot run.");
                var scopeRows = await ScalarAsync(source, "SELECT COUNT_BIG(*) FROM [dbo].[GIBS_FBClients] x WHERE x.ModuleID=@sourceModuleId", sourceModuleId);
                if (scopeRows == 0)
                    throw new ValidationException("The selected DNN ModuleID has no source clients. Check the source database and saved ModuleID.");
                await using var db = factory.CreateDbContext();
                if (!db.Database.IsSqlServer()) throw new ValidationException("Preflight currently supports SQL Server destinations only.");
                await db.Database.OpenConnectionAsync();
                var target = db.Database.GetDbConnection();
                if (selected.Any(map => map.Parent != null))
                {
                    var targetClients = await ColumnsAsync(target, "GIBSFBClients");
                    if (!HasInt(targetClients, "FBClientsId") || !HasInt(targetClients, "ModuleId"))
                        issues.Add(new("Blocker", "GIBSFBClients", "Destination client ID or module ownership metadata is missing or not visible; destination parent checks could not run."));
                    else if (!await IsIdentityPrimaryKeyAsync(target, "GIBSFBClients", "FBClientsId"))
                        issues.Add(new("Blocker", "GIBSFBClients", "Destination client ID is not a single-column identity primary key or its metadata is not visible; destination parent checks could not run."));
                    else
                    {
                        var parents = await CompareDestinationClientsAsync(source, target, sourceModuleId, destination.ModuleId);
                        if (parents.Missing > 0)
                            issues.Add(new("Warning", "GIBSFBClients", $"{parents.Missing:N0} scoped source client IDs do not yet exist in the destination. Import parents before dependent rows and recheck them at write time."));
                        if (parents.WrongModule > 0)
                            issues.Add(new("Blocker", "GIBSFBClients", $"{parents.WrongModule:N0} scoped source client IDs already belong to another destination module. They cannot be used as parents."));
                    }
                }
                foreach (var map in selected)
                {
                    if (map.Source.EndsWith("_Merged", StringComparison.Ordinal))
                    {
                        issues.Add(new("Blocker", map.Source, "Scoped archive mapping and duplicate/reimport checks are not implemented. Omit merged tables for the first import; no unfiltered archive counts are reported."));
                        results.Add(new(map.Source, map.Destination, map.SourceKey, map.DestinationKey, null, null));
                        continue;
                    }
                    var sourceColumns = await ColumnsAsync(source, map.Source);
                    var targetColumns = await ColumnsAsync(target, map.Destination);
                    var integrity = await CheckDestinationIntegrityAsync(target, map.Destination);
                    if (integrity.UnsafeConstraints > 0)
                        issues.Add(new("Blocker", map.Source, $"Destination {map.Destination} has {integrity.UnsafeConstraints:N0} disabled or untrusted check/foreign-key constraints. Enable and validate them before importing."));
                    if (integrity.DisabledIndexes > 0)
                        issues.Add(new("Blocker", map.Source, $"Destination {map.Destination} has {integrity.DisabledIndexes:N0} disabled unique indexes. Rebuild them before importing."));
                    if (integrity.DisabledTriggers > 0)
                        issues.Add(new("Blocker", map.Source, $"Destination {map.Destination} has {integrity.DisabledTriggers:N0} disabled triggers. Review and enable them before importing."));
                    var sourceKeyOk = HasInt(sourceColumns, map.SourceKey);
                    var targetKeyOk = HasInt(targetColumns, map.DestinationKey);
                    if (!sourceKeyOk) issues.Add(new("Blocker", map.Source, $"Source table or integer key {map.SourceKey} is missing or not visible."));
                    if (!targetKeyOk) issues.Add(new("Blocker", map.Source, $"Destination table or integer column {map.DestinationKey} is missing or not visible."));
                    long? rows = null, collisions = null;
                    var sourceScopeOk = map.Parent == null || HasInt(sourceColumns, map.Parent);
                    if (!sourceScopeOk)
                        issues.Add(new("Blocker", map.Source, "Source ClientID is missing or not an integer; scoped counts and collisions could not be checked."));
                    if (sourceColumns.Count > 0 && sourceScopeOk)
                        rows = await ScalarAsync(source, $"SELECT COUNT_BIG(*) FROM [dbo].{Quote(map.Source)} x WHERE {SourceScope(map)}", sourceModuleId);
                    if (sourceKeyOk && targetKeyOk && sourceScopeOk)
                    {
                        if (!await IsIdentityPrimaryKeyAsync(target, map.Destination, map.DestinationKey))
                            issues.Add(new("Blocker", map.Source, $"Destination {map.DestinationKey} is not a single-column identity primary key or its metadata is not visible."));
                        var counts = await CompareKeysAsync(source, target, map, sourceModuleId);
                        rows = counts.Rows;
                        collisions = counts.Collisions;
                        if (counts.Invalid > 0) issues.Add(new("Blocker", map.Source, $"{counts.Invalid:N0} rows have a null, nonpositive, or duplicate source ID."));
                        if (counts.Collisions > 0) issues.Add(new("Blocker", map.Source, $"{counts.Collisions:N0} source IDs already exist in the destination table."));
                    }
                    if (map.Parent != null && map.Source != "GIBS_FBxMas")
                    {
                        var parentColumns = await ColumnsAsync(source, map.ParentTable);
                        if (!HasInt(sourceColumns, map.Parent) || !HasInt(parentColumns, map.ParentKey))
                            issues.Add(new("Blocker", map.Source, $"The {map.Parent} parent relationship could not be checked because a column or parent table is missing."));
                        else
                        {
                            var missing = await MissingParentsAsync(source, map.Source, map.Parent, map.ParentTable, map.ParentKey);
                            if (ApprovedTurkeyOrphans(map, sourceModuleId, missing))
                                issues.Add(new("Warning", map.Source, $"{missing:N0} source-wide parentless turkey rows are excluded under the approved policy for ModuleID 412. Their module cannot be determined; they are not included in scoped counts or imported. No source rows are deleted; retain the frozen DNN backup as their archive of record. Verify this is the reviewed source snapshot."));
                            else if (missing > 0)
                                issues.Add(new("Blocker", map.Source, $"{missing:N0} source-wide orphan rows cannot be assigned to DNN ModuleID {sourceModuleId}; they are excluded from scoped counts. Retain them in an exception/archive workflow pending ownership review; exception storage and resolution are not implemented."));
                        }
                    }
                    if (map.Source == "GIBS_FBxMas") await CheckChristmasAsync(source, sourceColumns, plan, issues);
                    var fields = sourceScopeOk && sourceColumns.Count > 0
                        ? await CheckFieldsAsync(source, target, map, sourceModuleId, issues)
                        : null;
                    results.Add(new(map.Source, map.Destination, map.SourceKey, map.DestinationKey, rows, collisions, fields));
                }
                if (plan.Tables.Contains("GIBS_FBClients", StringComparer.Ordinal))
                {
                    var cols = await ColumnsAsync(source, "GIBS_FBClients");
                    if (HasInt(cols, "ModuleID") && HasInt(cols, "PortalID"))
                    {
                        var scopes = await ScalarAsync(source, "SELECT COUNT_BIG(*) FROM (SELECT DISTINCT [PortalID] FROM [dbo].[GIBS_FBClients] WHERE ModuleID=@sourceModuleId) AS scopes", sourceModuleId);
                        if (scopes > 1) issues.Add(new("Blocker", "GIBS_FBClients", $"Selected DNN ModuleID {sourceModuleId} spans {scopes:N0} portal values; review source scope before import."));
                    }
                    else issues.Add(new("Blocker", "GIBS_FBClients", "DNN module/portal scope columns could not be checked."));
                }
            }
            catch (Exception ex) when (ex is SqlException or ArgumentException or InvalidOperationException)
            {
                logger.Log(LogLevel.Error, this, LogFunction.Read, "Import preflight database failure for module {ModuleId}: {ErrorType}", moduleId, ex.GetType().Name);
                throw new ImportDataException("Preflight could not read source or destination data. Check database permissions, connectivity, timeouts, and server logs.");
            }
            return new(plan.Source, destination.ModuleId, results, issues.OrderBy(x => x.Severity == "Blocker" ? 0 : 1).ToList(), sourceModuleId);
        }

        private static async Task<Dictionary<string, string>> ColumnsAsync(DbConnection connection, string table)
        {
            await using var command = connection.CreateCommand();
            command.CommandTimeout = 120;
            command.CommandText = "SELECT c.name, ty.name FROM sys.columns c JOIN sys.tables t ON c.object_id=t.object_id JOIN sys.schemas s ON t.schema_id=s.schema_id JOIN sys.types ty ON c.system_type_id=ty.system_type_id AND ty.user_type_id=ty.system_type_id WHERE s.name=@schema AND t.name=@table";
            var schema = command.CreateParameter(); schema.ParameterName = "@schema"; schema.Value = "dbo"; command.Parameters.Add(schema);
            var name = command.CreateParameter(); name.ParameterName = "@table"; name.Value = table; command.Parameters.Add(name);
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync()) result.Add(reader.GetString(0), reader.GetString(1));
            return result;
        }

        private static async Task<bool> IsIdentityPrimaryKeyAsync(DbConnection connection, string table, string column, DbTransaction transaction = null)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandTimeout = 120;
            command.CommandText = "SELECT COUNT_BIG(*) FROM sys.tables t JOIN sys.schemas s ON s.schema_id=t.schema_id JOIN sys.indexes i ON i.object_id=t.object_id AND i.is_primary_key=1 JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id AND ic.key_ordinal>0 JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE s.name=@schema AND t.name=@table AND c.name=@column AND c.is_identity=1 AND NOT EXISTS (SELECT 1 FROM sys.index_columns other WHERE other.object_id=i.object_id AND other.index_id=i.index_id AND other.key_ordinal>0 AND other.column_id<>c.column_id)";
            var schema = command.CreateParameter(); schema.ParameterName = "@schema"; schema.Value = "dbo"; command.Parameters.Add(schema);
            var tableParameter = command.CreateParameter(); tableParameter.ParameterName = "@table"; tableParameter.Value = table; command.Parameters.Add(tableParameter);
            var columnParameter = command.CreateParameter(); columnParameter.ParameterName = "@column"; columnParameter.Value = column; command.Parameters.Add(columnParameter);
            return Convert.ToInt64(await command.ExecuteScalarAsync()) == 1;
        }

        private static bool HasInt(Dictionary<string, string> columns, string name) => columns.TryGetValue(name, out var type) && type == "int";
        private static string Quote(string identifier) => "[" + identifier.Replace("]", "]]") + "]";
        private static bool ApprovedTurkeyOrphans(TableMap map, int sourceModuleId, long count) =>
            map.Source == "GIBS_FBTurkeyRecipients" && sourceModuleId == 412 && count == 96;
        private static string SourceScope(TableMap map) => map.Parent == null
            ? "x.ModuleID=@sourceModuleId"
            : $"EXISTS (SELECT 1 FROM [dbo].[GIBS_FBClients] scopeClient WHERE scopeClient.ClientID=x.{Quote(map.Parent)} AND scopeClient.ModuleID=@sourceModuleId)";

        private static void AddSourceModuleParameter(DbCommand command, int sourceModuleId)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = "@sourceModuleId";
            parameter.DbType = System.Data.DbType.Int32;
            parameter.Value = sourceModuleId;
            command.Parameters.Add(parameter);
        }

        private static async Task<long> ScalarAsync(DbConnection connection, string sql, int? sourceModuleId = null)
        {
            await using var command = connection.CreateCommand();
            command.CommandTimeout = 120;
            command.CommandText = sql;
            if (sourceModuleId.HasValue) AddSourceModuleParameter(command, sourceModuleId.Value);
            return Convert.ToInt64(await command.ExecuteScalarAsync());
        }
        private static Task<long> MissingParentsAsync(DbConnection connection, string child, string parentColumn, string parent, string parentKey) =>
            ScalarAsync(connection, $"SELECT COUNT_BIG(*) FROM [dbo].{Quote(child)} c LEFT JOIN [dbo].{Quote(parent)} p ON c.{Quote(parentColumn)}=p.{Quote(parentKey)} WHERE p.{Quote(parentKey)} IS NULL");

        private const string ChristmasPolicySql = """
            SELECT disposition, COUNT_BIG(*)
            FROM (
                SELECT CASE
                    WHEN NOT EXISTS (SELECT 1 FROM [dbo].[GIBS_FBClients] c WHERE c.ClientID=x.ClientID)
                        THEN 'MissingClient'
                    WHEN x.ClAddFamMemID IS NULL
                        THEN 'ClientLevel'
                    WHEN EXISTS (SELECT 1 FROM [dbo].[GIBS_FBClientsAddFamMem] f WHERE f.ClAddFamMemID=x.ClAddFamMemID AND f.ClientID=x.ClientID)
                        THEN 'FamilyLinked'
                    WHEN EXISTS (SELECT 1 FROM [dbo].[GIBS_FBClientsAddFamMem] f WHERE f.ClAddFamMemID=x.ClAddFamMemID)
                        THEN 'ConflictingFamily'
                    WHEN EXISTS (SELECT 1 FROM [dbo].[GIBS_FBClientsAddFamMem_Merged] f WHERE f.ClAddFamMemID=x.ClAddFamMemID)
                        THEN 'ArchivedFamilyReview'
                    ELSE 'ClientLevel'
                END AS disposition
                FROM [dbo].[GIBS_FBxMas] x
                WHERE EXISTS (SELECT 1 FROM [dbo].[GIBS_FBClients] scopeClient WHERE scopeClient.ClientID=x.ClientID AND scopeClient.ModuleID=@sourceModuleId)
            ) classified
            GROUP BY disposition
            """;

        private static async Task CheckChristmasAsync(DbConnection source, Dictionary<string, string> columns, ImportDataPlan plan, List<ImportPreflightIssue> issues)
        {
            var clients = await ColumnsAsync(source, "GIBS_FBClients");
            var family = await ColumnsAsync(source, "GIBS_FBClientsAddFamMem");
            var archivedFamily = await ColumnsAsync(source, "GIBS_FBClientsAddFamMem_Merged");
            if (!HasInt(columns, "ClAddFamMemID") || !HasInt(columns, "ClientID") || !HasInt(clients, "ClientID")
                || !HasInt(family, "ClAddFamMemID") || !HasInt(family, "ClientID") || !HasInt(archivedFamily, "ClAddFamMemID"))
            {
                issues.Add(new("Blocker", "GIBS_FBxMas", "Christmas preservation policy could not be checked. Client, family, or merged-family integer ID columns are missing or not visible."));
                return;
            }
            var unassigned = await MissingParentsAsync(source, "GIBS_FBxMas", "ClientID", "GIBS_FBClients", "ClientID");
            if (unassigned > 0)
                issues.Add(new("Warning", "GIBS_FBxMas", $"{unassigned:N0} source-wide parentless Christmas rows are explicitly skipped under the approved import policy. Their DNN module cannot be determined; they are excluded from scoped counts and will not be imported into Oqtane. No source records are deleted. Retain the DNN backup as their archive of record; reconciliation must report them separately as skipped."));
            await using var command = source.CreateCommand();
            command.CommandTimeout = 120;
            command.CommandText = ChristmasPolicySql;
            AddSourceModuleParameter(command, plan.SourceModuleId.Value);
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var count = reader.GetInt64(1);
                switch (reader.GetString(0))
                {
                    case "FamilyLinked":
                        break;
                    case "ClientLevel":
                        issues.Add(new("Warning", "GIBS_FBxMas", $"{count:N0} rows are designated client-level Christmas history: leave FBClientsAddFamMemId null and preserve the original ClAddFamMemID in LegacyClAddFamMemId. No source records will be deleted. This report does not execute the transformation."));
                        break;
                    case "ArchivedFamilyReview":
                        issues.Add(new("Warning", "GIBS_FBxMas", $"{count:N0} rows have a valid client and a family ID found only in merged history: leave the active family link null, retain LegacyClAddFamMemId for archive review, and do not recreate active family members. This report does not execute the transformation."));
                        if (!plan.Tables.Contains("GIBS_FBClientsAddFamMem_Merged", StringComparer.Ordinal))
                            issues.Add(new("Warning", "GIBS_FBxMas", "Merged-family import is omitted. Retain the DNN backup as the archive of record and use it to review the preserved legacy family IDs; these archive records will not be available in Oqtane."));
                        break;
                    case "MissingClient":
                        issues.Add(new("Warning", "GIBS_FBxMas", $"{count:N0} Christmas rows have no matching source client and are explicitly skipped under the approved import policy. Retain them in the DNN backup and report them separately as skipped during reconciliation. No source records are deleted."));
                        break;
                    case "ConflictingFamily":
                        issues.Add(new("Blocker", "GIBS_FBxMas", $"{count:N0} rows reference an active family member belonging to another client. Review these relationships; the preservation policy does not authorize changing client IDs."));
                        break;
                    default:
                        throw new InvalidOperationException("Unexpected Christmas preservation classification.");
                }
            }
        }

        private static async Task<(long Missing, long WrongModule)> CompareDestinationClientsAsync(DbConnection source, DbConnection target, int sourceModuleId, int destinationModuleId)
        {
            await using var sourceCommand = source.CreateCommand();
            sourceCommand.CommandTimeout = 120;
            sourceCommand.CommandText = "SELECT DISTINCT ClientID FROM [dbo].[GIBS_FBClients] WHERE ModuleID=@sourceModuleId AND ClientID IS NOT NULL ORDER BY ClientID";
            AddSourceModuleParameter(sourceCommand, sourceModuleId);
            await using var targetCommand = target.CreateCommand();
            targetCommand.CommandTimeout = 120;
            targetCommand.CommandText = "SELECT FBClientsId, ModuleId FROM [dbo].[GIBSFBClients] ORDER BY FBClientsId";
            await using var sourceReader = await sourceCommand.ExecuteReaderAsync();
            await using var targetReader = await targetCommand.ExecuteReaderAsync();
            var hasTarget = await targetReader.ReadAsync();
            long missing = 0, wrongModule = 0;
            while (await sourceReader.ReadAsync())
            {
                var id = sourceReader.GetInt32(0);
                while (hasTarget && targetReader.GetInt32(0) < id) hasTarget = await targetReader.ReadAsync();
                if (!hasTarget || targetReader.GetInt32(0) != id) missing++;
                else if (targetReader.IsDBNull(1) || targetReader.GetInt32(1) != destinationModuleId) wrongModule++;
            }
            return (missing, wrongModule);
        }

        private static async Task<(long Rows, long Collisions, long Invalid)> CompareKeysAsync(DbConnection source, DbConnection target, TableMap map, int sourceModuleId)
        {
            await using var sourceCommand = source.CreateCommand();
            sourceCommand.CommandTimeout = 120;
            sourceCommand.CommandText = $"SELECT x.{Quote(map.SourceKey)} FROM [dbo].{Quote(map.Source)} x WHERE {SourceScope(map)} ORDER BY x.{Quote(map.SourceKey)}";
            AddSourceModuleParameter(sourceCommand, sourceModuleId);
            await using var targetCommand = target.CreateCommand();
            targetCommand.CommandTimeout = 120;
            targetCommand.CommandText = $"SELECT {Quote(map.DestinationKey)} FROM [dbo].{Quote(map.Destination)} ORDER BY {Quote(map.DestinationKey)}";
            await using var sourceReader = await sourceCommand.ExecuteReaderAsync();
            await using var targetReader = await targetCommand.ExecuteReaderAsync();
            var hasTarget = await targetReader.ReadAsync();
            long rows = 0, collisions = 0, invalid = 0;
            int? previous = null;
            while (await sourceReader.ReadAsync())
            {
                rows++;
                if (sourceReader.IsDBNull(0)) { invalid++; continue; }
                var id = sourceReader.GetInt32(0);
                if (id <= 0 || previous == id) invalid++;
                previous = id;
                while (hasTarget && targetReader.GetInt32(0) < id) hasTarget = await targetReader.ReadAsync();
                if (hasTarget && targetReader.GetInt32(0) == id) collisions++;
            }
            return (rows, collisions, invalid);
        }
    }
}
