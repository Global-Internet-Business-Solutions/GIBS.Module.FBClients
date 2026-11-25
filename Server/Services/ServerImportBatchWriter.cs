using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace GIBS.Module.FBClients.ImportData
{
    public partial class ServerImportDataService
    {
        // Application access is restricted to the explicitly configured isolated rehearsal.
        private static Task<ImportBatchCheckpoint.BatchProgress> WriteImportBatchAsync(SqlConnection source, SqlConnection target,
            int importModuleId, int destinationModuleId, int sourceModuleId, IReadOnlyList<string> selectedTables,
            string auditUser, int batchSize) => WriteImportBatchesAsync(source, target, importModuleId, destinationModuleId,
                sourceModuleId, selectedTables, auditUser, batchSize, 1, CancellationToken.None);

        private static async Task<ImportBatchCheckpoint.BatchProgress> WriteImportBatchesAsync(SqlConnection source, SqlConnection target,
            int importModuleId, int destinationModuleId, int sourceModuleId, IReadOnlyList<string> selectedTables,
            string auditUser, int batchSize, int maxBatches, CancellationToken cancellationToken,
            Func<Task<ImportBatchCheckpoint.SourceSnapshot>> readSourceIdentity = null,
            string requiredCompletedIdentity = null, int requiredCompletedTables = 0, string supplementaryRunKey = null)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (source == null || target == null || selectedTables == null || selectedTables.Count == 0
                || selectedTables.Distinct(StringComparer.Ordinal).Count() != selectedTables.Count
                || selectedTables.Any(name => !Mappings.Any(map => map.Source == name && !name.EndsWith("_Merged", StringComparison.Ordinal)))
                || string.IsNullOrWhiteSpace(auditUser) || auditUser.Length > 256 || batchSize is < 1 or > 1000 || maxBatches is < 1 or > 1000)
                throw new ValidationException("The batch import configuration is invalid or contains unsupported tables.");
            var selected = Mappings.Where(map => selectedTables.Contains(map.Source, StringComparer.Ordinal)).ToArray();
            if (requiredCompletedIdentity != null && (selected.Length != 1 || selected[0].Source != "GIBS_FBTurkeyRecipients" || requiredCompletedTables <= 0))
                throw new ValidationException("The supplementary rehearsal selection is invalid.");
            if (requiredCompletedIdentity == null && selected.Any(map => map.Parent != null) && !selected.Any(map => map.Source == "GIBS_FBClients"))
                throw new ValidationException("The client table must be selected when importing dependent tables.");
            if (selected.Any(map => map.Source == "GIBS_FBxMas") && !selected.Any(map => map.Source == "GIBS_FBClientsAddFamMem"))
                throw new ValidationException("The family table must be selected when importing Christmas records.");
            var orderedTables = selected.Select(map => map.Source).ToArray();
            var initialSourceIdentity = readSourceIdentity == null ? await ImportBatchCheckpoint.ReadSnapshotAsync(source) : await readSourceIdentity();
            var clientColumns = await ColumnsAsync(source, "GIBS_FBClients");
            if (!HasInt(clientColumns, "ClientID") || !HasInt(clientColumns, "ModuleID") || !HasInt(clientColumns, "PortalID")
                || await ScalarAsync(source, "SELECT COUNT_BIG(*) FROM dbo.GIBS_FBClients WHERE ModuleID=@sourceModuleId", sourceModuleId) == 0
                || await ScalarAsync(source, "SELECT COUNT_BIG(*) FROM (SELECT DISTINCT PortalID FROM dbo.GIBS_FBClients WHERE ModuleID=@sourceModuleId) s", sourceModuleId) != 1)
                throw new ValidationException("The source client scope is missing, empty, or spans multiple portals.");
            var shapes = new Dictionary<string, Dictionary<string, ColumnShape>>(StringComparer.Ordinal);
            foreach (var map in selected)
            {
                var sourceColumns = await ColumnsAsync(source, map.Source);
                var targetColumns = await ColumnsAsync(target, map.Destination);
                var integrity = await CheckDestinationIntegrityAsync(target, map.Destination);
                if (integrity.UnsafeConstraints > 0 || integrity.DisabledIndexes > 0 || integrity.DisabledTriggers > 0)
                    throw new ValidationException($"Destination {map.Destination} has disabled or untrusted constraints, disabled unique indexes, or disabled triggers; import is blocked.");
                shapes.Add(map.Source, await ColumnShapesAsync(target, map.Destination));
                if (!HasInt(sourceColumns, map.SourceKey) || !HasInt(targetColumns, map.DestinationKey)
                    || !await IsIdentityPrimaryKeyAsync(target, map.Destination, map.DestinationKey)
                    || (map.Parent != null && (!HasInt(sourceColumns, map.Parent) || !HasInt(targetColumns, "FBClientsId"))))
                    throw new ValidationException($"Required key or parent metadata for {map.Source} is invalid.");
                var issues = new List<ImportPreflightIssue>();
                await CheckFieldsAsync(source, target, map, sourceModuleId, issues);
                if (issues.Any(issue => issue.Severity == "Blocker"))
                    throw new ValidationException($"Field validation failed for {map.Source}: {issues.First(issue => issue.Severity == "Blocker").Message}");
                var counts = await CompareKeysAsync(source, target, map, sourceModuleId);
                if (counts.Invalid > 0) throw new ValidationException($"{map.Source} has invalid or duplicate scoped IDs.");
                if (map.Parent != null && map.Source != "GIBS_FBxMas")
                {
                    var missing = await MissingParentsAsync(source, map.Source, map.Parent, map.ParentTable, map.ParentKey);
                    if (missing > 0 && !ApprovedTurkeyOrphans(map, sourceModuleId, missing))
                        throw new ValidationException($"{map.Source} has {missing:N0} unassigned source-wide orphan records requiring review; only the 96 reviewed turkey rows for ModuleID 412 are approved exclusions.");
                }
                if (map.Source == "GIBS_FBxMas")
                {
                    var christmasIssues = new List<ImportPreflightIssue>();
                    await CheckChristmasAsync(source, sourceColumns, new ImportDataPlan("snapshot", orderedTables.ToList(), sourceModuleId), christmasIssues);
                    if (christmasIssues.Any(issue => issue.Severity == "Blocker"))
                        throw new ValidationException($"Christmas validation failed: {christmasIssues.First(issue => issue.Severity == "Blocker").Message}");
                }
            }
            var planIdentity = BatchPlanIdentity(selected, auditUser, batchSize);
            ImportBatchCheckpoint.BatchProgress result = null;
            var atomicChunk = readSourceIdentity != null;
            var transactionCount = atomicChunk ? 1 : maxBatches;
            for (var batchNumber = 0; batchNumber < transactionCount; batchNumber++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                foreach (var map in selected)
                {
                    var current = await ColumnShapesAsync(target, map.Destination);
                    var expected = shapes[map.Source];
                    if (current.Count != expected.Count || expected.Any(column => !current.TryGetValue(column.Key, out var shape) || shape != column.Value)
                        || !await IsIdentityPrimaryKeyAsync(target, map.Destination, map.DestinationKey))
                        throw new ValidationException($"Destination schema changed for {map.Destination}; stop and review the import before resuming.");
                }
                cancellationToken.ThrowIfCancellationRequested();
                result = await ImportBatchCheckpoint.ExecuteAsync(source, target, importModuleId, destinationModuleId, sourceModuleId, orderedTables, planIdentity,
                    async (connection, transaction, progress) =>
                    {
                        if (progress.TableIndex == 0 && progress.LastKey == 0 && progress.ImportedRows == 0)
                            await EnsureEmptyDestinationAsync(source, connection, transaction, selected, sourceModuleId, destinationModuleId);
                        var map = selected[progress.TableIndex];
                        if (atomicChunk)
                            return await ImportBatchCheckpoint.WriteChunkAsync(progress.LastKey, batchSize, maxBatches,
                                lastKey => WriteMappedBatchAsync(source, connection, transaction, map, shapes[map.Source],
                                    sourceModuleId, destinationModuleId, auditUser, lastKey, batchSize), cancellationToken);
                        return await WriteMappedBatchAsync(source, connection, transaction, map, shapes[map.Source], sourceModuleId, destinationModuleId,
                            auditUser, progress.LastKey, batchSize);
                    }, readSourceIdentity, readSourceIdentity == null ? null : initialSourceIdentity, cancellationToken, requiredCompletedIdentity, requiredCompletedTables, supplementaryRunKey);
                if (result.Completed) break;
            }
            return result;
        }

        // The serialized shape must remain stable for already persisted checkpoints.
        private static string BatchPlanIdentity(IEnumerable<TableMap> selected, string auditUser, int batchSize) =>
            JsonSerializer.Serialize(new { PolicyVersion = selected.Any(map => map.Source == "GIBS_FBTurkeyRecipients") ? 2 : 1, AuditUser = auditUser, BatchSize = batchSize,
                Tables = selected.Select(map => new { Map = map, Fields = FieldMappings(map) }).ToArray() });

        private static async Task EnsureEmptyDestinationAsync(SqlConnection source, SqlConnection target, SqlTransaction transaction,
            IReadOnlyList<TableMap> selected, int sourceModuleId, int destinationModuleId)
        {
            foreach (var map in selected)
            {
                await using var targetCommand = target.CreateCommand();
                targetCommand.Transaction = transaction;
                targetCommand.CommandTimeout = 120;
                var parentJoin = map.Parent == null ? "" : " LEFT JOIN dbo.GIBSFBClients p WITH (UPDLOCK,HOLDLOCK) ON p.FBClientsId=x.FBClientsId";
                var owner = map.Parent == null ? "x.ModuleId" : "p.ModuleId";
                targetCommand.CommandText = $"SELECT x.{Quote(map.DestinationKey)}, {owner} FROM dbo.{Quote(map.Destination)} x WITH (UPDLOCK,HOLDLOCK){parentJoin} ORDER BY x.{Quote(map.DestinationKey)}";
                await using var sourceCommand = source.CreateCommand();
                sourceCommand.CommandTimeout = 120;
                sourceCommand.CommandText = $"SELECT x.{Quote(map.SourceKey)} FROM dbo.{Quote(map.Source)} x WHERE {SourceScope(map)} ORDER BY x.{Quote(map.SourceKey)}";
                AddSourceModuleParameter(sourceCommand, sourceModuleId);
                await using var sourceReader = await sourceCommand.ExecuteReaderAsync();
                await using var targetReader = await targetCommand.ExecuteReaderAsync();
                var hasSource = await sourceReader.ReadAsync();
                while (await targetReader.ReadAsync())
                {
                    if (targetReader.IsDBNull(1))
                        throw new ValidationException($"Destination ownership is unknown for a row in {map.Destination}; review it before importing.");
                    if (targetReader.GetInt32(1) == destinationModuleId)
                        throw new ValidationException($"The destination module already contains rows in {map.Destination}; a fresh destination is required.");
                    var targetKey = targetReader.GetInt32(0);
                    while (hasSource && sourceReader.GetInt32(0) < targetKey) hasSource = await sourceReader.ReadAsync();
                    if (hasSource && sourceReader.GetInt32(0) == targetKey)
                        throw new ValidationException($"A source ID already exists in destination table {map.Destination}.");
                }
            }
        }

        private static async Task<ImportBatchCheckpoint.BatchResult> WriteMappedBatchAsync(SqlConnection source, SqlConnection target,
            SqlTransaction transaction, TableMap map, Dictionary<string, ColumnShape> targetShapes, int sourceModuleId, int destinationModuleId, string auditUser, int lastKey, int batchSize)
        {
            var integrity = await CheckDestinationIntegrityAsync(target, map.Destination, transaction);
            if (integrity.UnsafeConstraints > 0 || integrity.DisabledIndexes > 0 || integrity.DisabledTriggers > 0)
                throw new ValidationException($"Destination integrity metadata changed for {map.Destination}; no batch changes were committed.");
            var sourceColumns = await ColumnsAsync(source, map.Source);
            var fields = FieldMappings(map);
            var christmas = map.Source == "GIBS_FBxMas";
            var familyDisposition = "CASE WHEN x.ClAddFamMemID IS NULL THEN 2 "
                + "WHEN EXISTS (SELECT 1 FROM dbo.GIBS_FBClientsAddFamMem f WHERE f.ClAddFamMemID=x.ClAddFamMemID AND f.ClientID=x.ClientID) THEN 1 "
                + "WHEN EXISTS (SELECT 1 FROM dbo.GIBS_FBClientsAddFamMem f WHERE f.ClAddFamMemID=x.ClAddFamMemID) THEN 4 "
                + "WHEN EXISTS (SELECT 1 FROM dbo.GIBS_FBClientsAddFamMem_Merged f WHERE f.ClAddFamMemID=x.ClAddFamMemID) THEN 3 ELSE 2 END";
            var expressions = fields.Select(field =>
            {
                string value;
                if (field.Destination is "CreatedBy" or "ModifiedBy") value = "@auditUser";
                else if (field.Destination is "CreatedOn" or "ModifiedOn") value = "@timestamp";
                else if (field.Destination == "ModuleId") value = "@destinationModuleId";
                else if (christmas && field.Destination == "FBClientsAddFamMemId")
                    value = $"CASE WHEN ({familyDisposition})=1 THEN x.ClAddFamMemID ELSE NULL END";
                else if (map.Source == "GIBS_FBClients" && field.Destination == "PrimaryIncomeSource" && !sourceColumns.ContainsKey(field.Source))
                    value = "CONVERT(nvarchar(50),N'')";
                else value = "x." + Quote(field.Source);
                if (targetShapes[field.Destination].Type == "date") value = $"CONVERT(date,{value})";
                return value + " AS " + Quote(field.Destination);
            }).ToArray();
            var auxiliary = map.Parent == null ? "" : christmas
                ? $", x.ClientID AS [__parent], x.ClAddFamMemID AS [__family], {familyDisposition} AS [__disposition]"
                : ", x.ClientID AS [__parent]";
            await using var command = source.CreateCommand();
            command.CommandTimeout = 120;
            command.CommandText = $"SELECT TOP (@batchSize) {string.Join(",", expressions)}{auxiliary} FROM dbo.{Quote(map.Source)} x WHERE {SourceScope(map)} AND x.{Quote(map.SourceKey)}>@lastKey ORDER BY x.{Quote(map.SourceKey)}";
            command.Parameters.Add("@batchSize", SqlDbType.Int).Value = batchSize;
            command.Parameters.Add("@lastKey", SqlDbType.Int).Value = lastKey;
            command.Parameters.Add("@sourceModuleId", SqlDbType.Int).Value = sourceModuleId;
            command.Parameters.Add("@destinationModuleId", SqlDbType.Int).Value = destinationModuleId;
            command.Parameters.Add("@auditUser", SqlDbType.NVarChar, 256).Value = auditUser;
            command.Parameters.Add("@timestamp", SqlDbType.DateTime2).Value = DateTime.UtcNow;
            using var batch = new DataTable();
            var parents = new List<ImportBatchParentChecks.ParentReference>();
            var finalKey = lastKey;
            await using (var reader = await command.ExecuteReaderAsync())
            {
                for (var i = 0; i < fields.Count; i++) batch.Columns.Add(fields[i].Destination, reader.GetFieldType(i));
                var keyIndex = fields.FindIndex(field => field.Destination == map.DestinationKey);
                while (await reader.ReadAsync())
                {
                    var id = reader.GetInt32(keyIndex);
                    if (id <= finalKey) throw new ValidationException($"{map.Source} contains duplicate or invalid keys; no batch changes were committed.");
                    finalKey = id;
                    var values = new object[fields.Count];
                    for (var i = 0; i < values.Length; i++) values[i] = reader.GetValue(i);
                    batch.Rows.Add(values);
                    if (map.Parent != null)
                    {
                        var parent = reader.GetInt32(fields.Count);
                        int? family = christmas && !reader.IsDBNull(fields.Count + 1) ? reader.GetInt32(fields.Count + 1) : null;
                        var disposition = christmas ? (ImportBatchParentChecks.FamilyDisposition)reader.GetInt32(fields.Count + 2) : ImportBatchParentChecks.FamilyDisposition.NotChristmas;
                        parents.Add(new(parent, family, disposition));
                    }
                }
            }
            if (batch.Rows.Count == 0) return new(lastKey, 0, true);
            await ImportBatchParentChecks.ValidateAsync(target, transaction, destinationModuleId, sourceModuleId, parents);
            using var bulk = new SqlBulkCopy(target, SqlBulkCopyOptions.KeepIdentity | SqlBulkCopyOptions.KeepNulls | SqlBulkCopyOptions.CheckConstraints | SqlBulkCopyOptions.FireTriggers, transaction);
            bulk.DestinationTableName = "dbo." + Quote(map.Destination);
            bulk.BulkCopyTimeout = 120;
            foreach (var field in fields) bulk.ColumnMappings.Add(field.Destination, field.Destination);
            await bulk.WriteToServerAsync(batch);
            return new(finalKey, batch.Rows.Count, batch.Rows.Count < batchSize);
        }
    }
}
