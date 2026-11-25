using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using GIBS.Module.FBClients.Models;
using Microsoft.Data.SqlClient;

namespace GIBS.Module.FBClients.ImportData
{
    public partial class ServerImportDataService
    {
        private record FieldMap(string Destination, string Source, string Rule);
        private record ColumnShape(string Type, int Length, int Precision, int Scale, bool Nullable, string Collation);
        private record DestinationIntegrity(long UnsafeConstraints, long DisabledIndexes, long DisabledTriggers);

        private static async Task<DestinationIntegrity> CheckDestinationIntegrityAsync(DbConnection connection, string table, DbTransaction transaction = null)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandTimeout = 120;
            command.CommandText = "SELECT (SELECT COUNT_BIG(*) FROM sys.check_constraints c JOIN sys.tables t ON t.object_id=c.parent_object_id JOIN sys.schemas s ON s.schema_id=t.schema_id WHERE s.name='dbo' AND t.name=@table AND (c.is_disabled=1 OR c.is_not_trusted=1)) + (SELECT COUNT_BIG(*) FROM sys.foreign_keys f JOIN sys.tables t ON t.object_id=f.parent_object_id JOIN sys.schemas s ON s.schema_id=t.schema_id WHERE s.name='dbo' AND t.name=@table AND (f.is_disabled=1 OR f.is_not_trusted=1)), (SELECT COUNT_BIG(*) FROM sys.indexes i JOIN sys.tables t ON t.object_id=i.object_id JOIN sys.schemas s ON s.schema_id=t.schema_id WHERE s.name='dbo' AND t.name=@table AND i.is_unique=1 AND i.is_disabled=1), (SELECT COUNT_BIG(*) FROM sys.triggers tr JOIN sys.tables t ON t.object_id=tr.parent_id JOIN sys.schemas s ON s.schema_id=t.schema_id WHERE s.name='dbo' AND t.name=@table AND tr.is_disabled=1)";
            var parameter = command.CreateParameter();
            parameter.ParameterName = "@table";
            parameter.Value = table;
            command.Parameters.Add(parameter);
            await using var reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) throw new InvalidOperationException("Destination integrity inspection returned no result.");
            return new(reader.GetInt64(0), reader.GetInt64(1), reader.GetInt64(2));
        }

        private static Type SourceModel(TableMap map) => map.Source switch
        {
            "GIBS_FBClients" => typeof(Models.FBClients),
            "GIBS_FBClientsAddFamMem" => typeof(FBClientsAddFamMem),
            "GIBS_FBClientsVisits" => typeof(FBClientsVisits),
            "GIBS_FBClientsIncomeAndExpense" => typeof(FBClientsIncomeAndExpense),
            "GIBS_FBClientsTrueFalseQuestions" => typeof(FBClientsTrueFalseQuestions),
            "GIBS_FBClientsIDPhoto" => typeof(FBClientsIDPhoto),
            "GIBS_FBxMas" => typeof(FBClientsChristmas),
            "GIBS_FBTurkeyRecipients" => typeof(FBTurkeyRecipient),
            _ => throw new InvalidOperationException("No active field mapping for this source table.")
        };

        private static List<FieldMap> FieldMappings(TableMap map) => SourceModel(map).GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Select(property => MapField(map, property.Name)).ToList();

        private static FieldMap MapField(TableMap map, string destination)
        {
            if (destination is "CreatedBy" or "ModifiedBy")
                return new(destination, null, "Authenticated Oqtane importing administrator; DNN audit user IDs remain in legacy fields.");
            if (destination is "CreatedOn" or "ModifiedOn")
                return new(destination, null, "UTC import timestamp; original DNN audit timestamps remain in legacy fields without timezone conversion.");
            if (destination == "ModuleId")
                return new(destination, null, "Selected destination FBClients module; never copy the DNN ModuleID here.");
            if (destination == map.DestinationKey)
                return new(destination, map.SourceKey, "Preserve source primary ID using destination IDENTITY_INSERT.");
            if (destination == "FBClientsId")
                return new(destination, "ClientID", "Preserved client ID; require imported parent in the selected destination module.");
            if (destination == "FBClientsAddFamMemId")
                return new(destination, "ClAddFamMemID", "Use only if imported family belongs to the same imported client; otherwise null under Christmas preservation policy.");
            var source = destination switch
            {
                "LegacyClientId" => "ClientID",
                "LegacyModuleId" => "ModuleID",
                "LegacyPortalId" => "PortalID",
                "LegacyCaseWorkerUserId" => "ClientCaseWorker",
                "LegacyCreatedOnDate" => "CreatedOnDate",
                "LegacyCreatedByUserId" => "CreatedByUserID",
                "LegacyLastModifiedOnDate" => "LastModifiedOnDate",
                "LegacyLastModifiedByUserId" => "LastModifiedByUserID",
                "LegacyClAddFamMemId" => "ClAddFamMemID",
                "LegacyVisitId" => "VisitID",
                "LegacyIeId" => "IeID",
                "LegacyTfId" => "TfID",
                "LegacyImageId" => "ImageID",
                "LegacyXMasId" => "xMasID",
                _ => destination
            };
            return new(destination, source, "Copy without trimming, truncation, or null substitution; date-only fields retain the calendar date. Legacy IDs and audit values are preserved.");
        }

        private static async Task<Dictionary<string, ColumnShape>> ColumnShapesAsync(DbConnection connection, string table, DbTransaction transaction = null)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandTimeout = 120;
            command.CommandText = "SELECT c.name,ty.name,c.max_length,c.precision,c.scale,c.is_nullable,c.collation_name FROM sys.columns c JOIN sys.tables t ON t.object_id=c.object_id JOIN sys.schemas s ON s.schema_id=t.schema_id JOIN sys.types ty ON ty.user_type_id=c.user_type_id WHERE s.name='dbo' AND t.name=@table";
            var parameter = command.CreateParameter(); parameter.ParameterName = "@table"; parameter.Value = table; command.Parameters.Add(parameter);
            var result = new Dictionary<string, ColumnShape>(StringComparer.OrdinalIgnoreCase);
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                result.Add(reader.GetString(0), new(reader.GetString(1), reader.GetInt16(2), reader.GetByte(3), reader.GetByte(4), reader.GetBoolean(5), reader.IsDBNull(6) ? null : reader.GetString(6)));
            return result;
        }

        private static bool CompatibleType(string source, string target) => target switch
        {
            "nvarchar" or "varchar" or "nchar" or "char" => source is "nvarchar" or "varchar" or "nchar" or "char" or "text" or "ntext",
            "int" => source is "int" or "smallint" or "tinyint" or "bigint",
            "bit" => source == "bit",
            "date" or "datetime" or "datetime2" => source is "date" or "datetime" or "datetime2" or "smalldatetime",
            "decimal" or "numeric" => source is "decimal" or "numeric" or "money" or "smallmoney",
            "varbinary" or "binary" => source is "varbinary" or "binary" or "image",
            _ => false
        };

        private static string ValidationPredicate(FieldMap field, ColumnShape target, ColumnShape source)
        {
            var value = "x." + Quote(field.Source);
            var checks = new List<string>();
            if (!target.Nullable) checks.Add(value + " IS NULL");
            switch (target.Type)
            {
                case "nvarchar":
                case "nchar":
                    if (target.Length >= 0) checks.Add($"DATALENGTH(CONVERT(nvarchar(max),{value}))>{target.Length}");
                    break;
                case "varchar":
                case "char":
                    if (string.IsNullOrEmpty(target.Collation) || target.Collation.Any(c => !char.IsAsciiLetterOrDigit(c) && c != '_'))
                        throw new InvalidOperationException("Destination string collation metadata is invalid.");
                    var encoded = $"CONVERT(varchar(max),CONVERT(nvarchar(max),{value}) COLLATE {target.Collation})";
                    if (target.Length >= 0) checks.Add($"DATALENGTH({encoded})>{target.Length}");
                    checks.Add($"CONVERT(varbinary(max),CONVERT(nvarchar(max),{encoded}))<>CONVERT(varbinary(max),CONVERT(nvarchar(max),{value}))");
                    break;
                case "decimal":
                case "numeric":
                    var decimalValue = $"TRY_CONVERT(decimal({target.Precision},{target.Scale}),{value})";
                    checks.Add($"({value} IS NOT NULL AND ({decimalValue} IS NULL OR {decimalValue}<>{value}))");
                    break;
                case "int":
                case "date":
                    checks.Add($"({value} IS NOT NULL AND TRY_CONVERT({target.Type},{value}) IS NULL)");
                    break;
                case "datetime":
                case "datetime2":
                    var timestampType = target.Type == "datetime2" ? $"datetime2({target.Scale})" : "datetime";
                    var sourceTimestampType = source.Type == "datetime2" ? $"datetime2({source.Scale})" : source.Type;
                    var timestamp = $"TRY_CONVERT({timestampType},{value})";
                    var roundTrip = $"TRY_CONVERT({sourceTimestampType},{timestamp})";
                    checks.Add($"({value} IS NOT NULL AND ({timestamp} IS NULL OR {roundTrip} IS NULL OR {roundTrip}<>{value}))");
                    break;
                case "varbinary":
                case "binary":
                    if (target.Length >= 0) checks.Add($"DATALENGTH({value})>{target.Length}");
                    break;
            }
            return checks.Count == 0 ? "1=0" : string.Join(" OR ", checks);
        }

        private static async Task<bool> IncomeColumnAbsentAsync(DbConnection source)
        {
            await using var command = source.CreateCommand();
            command.CommandTimeout = 120;
            command.CommandText = "SELECT TOP (0) [PrimaryIncomeSource] FROM [dbo].[GIBS_FBClients]";
            try
            {
                await using var reader = await command.ExecuteReaderAsync();
                return false;
            }
            catch (SqlException ex) when (ex.Number == 207)
            {
                return true;
            }
        }

        private static async Task<List<ImportPreflightField>> CheckFieldsAsync(DbConnection source, DbConnection target, TableMap map,
            int sourceModuleId, List<ImportPreflightIssue> issues)
        {
            var mappings = FieldMappings(map);
            var sourceColumns = await ColumnShapesAsync(source, map.Source);
            var targetColumns = await ColumnShapesAsync(target, map.Destination);
            var results = new List<ImportPreflightField>();
            var checks = new List<(FieldMap Map, ColumnShape Target, string Predicate)>();
            foreach (var field in mappings)
            {
                if (!targetColumns.TryGetValue(field.Destination, out var targetColumn))
                {
                    issues.Add(new("Blocker", map.Source, $"Mapped destination column {field.Destination} is missing or not visible."));
                    results.Add(new(field.Source ?? "Generated", field.Destination, field.Rule, null));
                    continue;
                }
                if (field.Source == null)
                {
                    results.Add(new("Generated", field.Destination, field.Rule, null));
                    continue;
                }
                if (!sourceColumns.TryGetValue(field.Source, out var sourceColumn))
                {
                    if (map.Source == "GIBS_FBClients" && field.Source == "PrimaryIncomeSource"
                        && field.Destination == "PrimaryIncomeSource" && targetColumn.Type == "nvarchar"
                        && await IncomeColumnAbsentAsync(source))
                    {
                        const string rule = "Source PrimaryIncomeSource is absent: generate an empty string under the approved policy. Do not infer it from income history. Existing source values are never replaced by this rule.";
                        issues.Add(new("Warning", map.Source, rule + " Import transformation is not yet executed."));
                        results.Add(new("Generated: empty string", field.Destination, rule, 0));
                        continue;
                    }
                    issues.Add(new("Blocker", map.Source, $"Source column {field.Source} is missing or not visible. Destination {field.Destination} has no source value; a mapping or explicit generation policy is required."));
                    results.Add(new(field.Source, field.Destination, field.Rule, null));
                    continue;
                }
                if (!CompatibleType(sourceColumn.Type, targetColumn.Type))
                {
                    issues.Add(new("Blocker", map.Source, $"Source {field.Source} has type {sourceColumn.Type}, incompatible with destination {field.Destination} ({targetColumn.Type})."));
                    results.Add(new(field.Source, field.Destination, field.Rule, null));
                    continue;
                }
                if (field.Destination == "FBClientsAddFamMemId")
                {
                    results.Add(new(field.Source, field.Destination, field.Rule, null));
                    continue;
                }
                checks.Add((field, targetColumn, ValidationPredicate(field, targetColumn, sourceColumn)));
            }
            foreach (var unmapped in sourceColumns.Keys.Where(name => !mappings.Any(field => string.Equals(field.Source, name, StringComparison.OrdinalIgnoreCase))))
                issues.Add(new("Blocker", map.Source, $"Source column {unmapped} has no field mapping; it will not be silently discarded."));
            foreach (var unmapped in targetColumns.Keys.Where(name => !mappings.Any(field => string.Equals(field.Destination, name, StringComparison.OrdinalIgnoreCase))))
                issues.Add(new("Blocker", map.Source, $"Destination column {unmapped} has no field mapping or generation rule."));
            if (checks.Count == 0) return results;
            await using var command = source.CreateCommand();
            command.CommandTimeout = 120;
            command.CommandText = "SELECT " + string.Join(",", checks.Select(check => $"COUNT_BIG(CASE WHEN {check.Predicate} THEN 1 END)"))
                + $" FROM [dbo].{Quote(map.Source)} x WHERE {SourceScope(map)}";
            AddSourceModuleParameter(command, sourceModuleId);
            await using var reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) throw new InvalidOperationException("Field validation returned no aggregate row.");
            for (var i = 0; i < checks.Count; i++)
            {
                var check = checks[i];
                var invalid = reader.GetInt64(i);
                results.Add(new(check.Map.Source, check.Map.Destination, check.Map.Rule, invalid));
                if (invalid > 0)
                    issues.Add(new("Blocker", map.Source, $"{invalid:N0} scoped rows cannot map {check.Map.Source} to {check.Map.Destination} without a required-null, length, encoding, range, or decimal precision violation. No values were truncated or substituted."));
            }
            return results.OrderBy(field => field.Destination, StringComparer.Ordinal).ToList();
        }
    }
}
