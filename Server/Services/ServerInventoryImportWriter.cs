using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using GIBS.Module.FBClients.Models;

namespace GIBS.Module.FBClients.ImportData
{
    public partial class ServerImportDataService
    {
        public async Task<ImportBatchRunReport> RunInventoryRehearsalAsync(int moduleId, InventoryImportRunRequest request)
        {
            var (tenant, site) = await AuthorizeAsync(moduleId, edit: true);
            var plan = ReadInventoryPlan(moduleId);
            ValidateInventoryDestinations(plan, site, true);
            if (request == null || request.MaxBatches is < 1 or > RehearsalMaxBatches)
                throw new ValidationException($"Select between 1 and {RehearsalMaxBatches} batches per request.");
            var user = http.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true && authentication != null)
                user = (await authentication.GetAuthenticationStateAsync()).User;
            var auditUser = user?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(auditUser) || auditUser.Length > 256)
                throw new ValidationException("The authenticated importing administrator is unavailable or too long.");
            var cancellationToken = http.HttpContext?.RequestAborted ?? CancellationToken.None;
            return await InventoryDatabaseOperationAsync(moduleId, async () =>
            {
                await using var source = new SqlConnection(InventorySourceConnection(tenant, site, plan.Source, moduleId));
                await source.OpenAsync(cancellationToken);
                await using var db = factory.CreateDbContext();
                if (!db.Database.IsSqlServer()) throw new ValidationException("Inventory rehearsal supports SQL Server only.");
                await db.Database.OpenConnectionAsync(cancellationToken);
                if (db.Database.GetDbConnection() is not SqlConnection target) throw new ValidationException("The destination is not a SQL Server connection.");
                var selected = InventoryMappings.Where(map => plan.Tables.Contains(map.Source, StringComparer.Ordinal)).ToArray();
                var azureDatabase = configuration["FBClientsImport:Rehearsal:FrozenAzureDatabase"];
                if (!string.IsNullOrWhiteSpace(azureDatabase)
                    && (!bool.TryParse(configuration["FBClientsImport:Rehearsal:AzureCopyFreezeConfirmed"], out var frozen) || !frozen))
                    throw new ValidationException("The host must confirm the Azure source copy is frozen against all writers.");
                var fingerprints = selected.Select(map => (Table: map.Source, Key: map.Key)).ToList();
                string mainIdentity = null;
                var mainTableCount = 0;
                Func<Task<ImportBatchCheckpoint.SourceSnapshot>> readMainIdentity = null;
                ImportBatchCheckpoint.SourceSnapshot originalSnapshot = null;
                ImportBatchCheckpoint.SourceSnapshot initialCombined = null;
                List<(string Table, string Key)> originalFingerprints = null;
                if (plan.Tables.Contains("GIBS_FBClientsVisitItem", StringComparer.Ordinal))
                {
                    var mainPlan = (await GetConfigurationAsync(moduleId)).Plan;
                    if (mainPlan == null || mainPlan.Source != plan.Source || mainPlan.SourceModuleId != plan.SourceClientsModuleId
                        || !mainPlan.Tables.Contains("GIBS_FBClients", StringComparer.Ordinal) || !mainPlan.Tables.Contains("GIBS_FBClientsVisits", StringComparer.Ordinal)
                        || mainPlan.Tables.Any(name => !Mappings.Any(map => map.Source == name && !name.EndsWith("_Merged", StringComparison.Ordinal))))
                        throw new ValidationException("Visit items require the original saved client/visit plan for this same source and client scope. Complete its isolated rehearsal first; existing clients and visits are reused, never reimported by the inventory run.");
                    var originalMaps = Mappings.Where(map => mainPlan.Tables.Contains(map.Source, StringComparer.Ordinal)).ToArray();
                    originalFingerprints = originalMaps.Select(map => (Table: map.Source, Key: map.SourceKey)).ToList();
                    if (mainPlan.Tables.Contains("GIBS_FBxMas", StringComparer.Ordinal))
                        originalFingerprints.Add(("GIBS_FBClientsAddFamMem_Merged", "ClAddFamMemID"));
                    fingerprints.AddRange(originalFingerprints);
                    if (string.IsNullOrWhiteSpace(azureDatabase))
                    {
                        readMainIdentity = () => ImportBatchCheckpoint.ReadSnapshotAsync(source);
                        originalSnapshot = await readMainIdentity();
                    }
                    else
                    {
                        (originalSnapshot, initialCombined) = await ImportAzureFrozenSource.ReadPairAsync(source, azureDatabase,
                            originalFingerprints, fingerprints.Distinct().ToArray(), cancellationToken);
                    }
                    mainIdentity = ImportBatchCheckpoint.Identity(originalSnapshot, moduleId, plan.DestinationClientsModuleId,
                        plan.SourceClientsModuleId, originalMaps.Select(map => map.Source).ToArray(), BatchPlanIdentity(originalMaps, auditUser, RehearsalBatchSize));
                    mainTableCount = originalMaps.Length;
                }
                Func<Task<ImportBatchCheckpoint.SourceSnapshot>> readIdentity = async () =>
                {
                    if (initialCombined != null)
                    {
                        var initial = initialCombined;
                        initialCombined = null;
                        return initial;
                    }
                    if (originalFingerprints != null && !string.IsNullOrWhiteSpace(azureDatabase))
                    {
                        var (original, combined) = await ImportAzureFrozenSource.ReadPairAsync(source, azureDatabase,
                            originalFingerprints, fingerprints.Distinct().ToArray(), cancellationToken);
                        if (original != originalSnapshot)
                            throw new ValidationException("The original frozen source changed after its client/visit rehearsal. No inventory chunk was committed.");
                        return combined;
                    }
                    if (readMainIdentity != null && await readMainIdentity() != originalSnapshot)
                        throw new ValidationException("The original frozen source changed after its client/visit rehearsal. No inventory chunk was committed.");
                    return string.IsNullOrWhiteSpace(azureDatabase) ? await ImportBatchCheckpoint.ReadSnapshotAsync(source)
                        : await ImportAzureFrozenSource.ReadAsync(source, azureDatabase, fingerprints.Distinct().ToArray(), cancellationToken);
                };
                return await WriteInventoryBatchesAsync(source, target, moduleId, plan, auditUser, request.MaxBatches,
                    readIdentity, mainIdentity, mainTableCount, cancellationToken);
            });
        }

        private static async Task<ImportBatchRunReport> WriteInventoryBatchesAsync(SqlConnection source, SqlConnection target,
            int moduleId, InventoryImportPlan plan, string auditUser, int maxBatches,
            Func<Task<ImportBatchCheckpoint.SourceSnapshot>> readIdentity, string mainIdentity, int mainTableCount, CancellationToken cancellationToken)
        {
                plan = InventoryImportSelection.Normalize(plan);
                if (source == null || target == null || readIdentity == null || moduleId <= 0 || string.IsNullOrWhiteSpace(auditUser)
                    || auditUser.Length > 256 || maxBatches is < 1 or > RehearsalMaxBatches
                    || (plan.Tables.Contains("GIBS_FBClientsVisitItem", StringComparer.Ordinal) && (mainIdentity == null || mainTableCount <= 0)))
                    throw new ValidationException("The inventory batch configuration or required client/visit prerequisite is invalid.");
                var selected = InventoryMappings.Where(map => plan.Tables.Contains(map.Source, StringComparer.Ordinal)).ToArray();
                var snapshot = await readIdentity();
                var shapes = new Dictionary<string, Dictionary<string, ColumnShape>>(StringComparer.Ordinal);
                foreach (var map in selected)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var issues = new List<ImportPreflightIssue>();
                    await ValidateInventorySourceAsync(source, map, plan, issues);
                    await ValidateInventoryFieldsAsync(source, target, map, plan, issues);
                    if (issues.Any(issue => issue.Severity == "Blocker"))
                        throw new ValidationException($"Inventory validation failed: {issues.First(issue => issue.Severity == "Blocker").Table}: {issues.First(issue => issue.Severity == "Blocker").Message}");
                    if (!await IsIdentityPrimaryKeyAsync(target, map.Destination, map.Key))
                        throw new ValidationException($"Destination key for {map.Destination} is invalid.");
                    var integrity = await CheckDestinationIntegrityAsync(target, map.Destination);
                    if (integrity.UnsafeConstraints > 0 || integrity.DisabledIndexes > 0 || integrity.DisabledTriggers > 0)
                        throw new ValidationException($"Destination integrity metadata for {map.Destination} is unsafe.");
                    shapes.Add(map.Source, await ColumnShapesAsync(target, map.Destination));
                }
                var lineItemsMap = selected.FirstOrDefault(map => map.Source == "GIBS_FBLineItems");
                var missingLineItemsColumn = lineItemsMap != null && await MissingInventoryLineItemsColumnAsync(source, lineItemsMap);
                var mappings = selected.Select(map => new { map.Source, map.Destination, map.Key, Fields = InventoryFields(map) }).ToArray();
                var policy = lineItemsMap == null
                    ? JsonSerializer.Serialize(new { PolicyVersion = 1, Plan = plan, AuditUser = auditUser, BatchSize = RehearsalBatchSize, MainIdentity = mainIdentity, Fields = mappings })
                    : JsonSerializer.Serialize(new { PolicyVersion = 2, Plan = plan, AuditUser = auditUser, BatchSize = RehearsalBatchSize, MainIdentity = mainIdentity, Fields = mappings, MissingLineItemsColumn = missingLineItemsColumn });
                var progress = await ImportBatchCheckpoint.ExecuteAsync(source, target, moduleId, plan.DestinationInventoryModuleId,
                    plan.SourceInventoryModuleId, selected.Select(map => map.Source).ToArray(), policy,
                    async (connection, transaction, checkpoint) =>
                    {
                        if (mainIdentity != null) await ValidateInventoryMainCheckpointAsync(connection, transaction, moduleId, plan, mainIdentity, mainTableCount);
                        if (checkpoint.ImportedRows == 0 && checkpoint.TableIndex == 0 && checkpoint.LastKey == 0)
                            await EnsureInventoryEmptyAsync(source, connection, transaction, selected, plan);
                        var map = selected[checkpoint.TableIndex];
                        var current = await ColumnShapesAsync(connection, map.Destination, transaction);
                        var expected = shapes[map.Source];
                        if (current.Count != expected.Count || expected.Any(column => !current.TryGetValue(column.Key, out var shape) || shape != column.Value)
                            || !await IsIdentityPrimaryKeyAsync(connection, map.Destination, map.Key, transaction))
                            throw new ValidationException("Destination schema changed during inventory validation. Stop and review recovery.");
                        return await ImportBatchCheckpoint.WriteChunkAsync(checkpoint.LastKey, RehearsalBatchSize, maxBatches,
                            key => WriteInventoryBatchAsync(source, connection, transaction, map, plan, auditUser, key, missingLineItemsColumn, cancellationToken), cancellationToken);
                    }, readIdentity, snapshot, cancellationToken, independentRunKey: plan.RunKey);
                return new ImportBatchRunReport(progress.ImportedRows, progress.TableIndex, selected.Length, progress.Completed);
        }

        private static async Task ValidateInventoryMainCheckpointAsync(SqlConnection target, SqlTransaction transaction, int importModuleId,
            InventoryImportPlan plan, string identity, int tableCount)
        {
            await using var command = target.CreateCommand();
            command.Transaction = transaction;
            command.CommandTimeout = 120;
            command.CommandText = "SELECT COUNT_BIG(*) FROM dbo.GIBSFBClientsImportCheckpoint WITH (UPDLOCK,HOLDLOCK) WHERE DestinationModuleId=@clients AND RunKey='1main' AND ImportModuleId=@import AND RunIdentity=@identity AND TableIndex=@tables AND LastKey=0 AND Completed=1";
            command.Parameters.Add("@clients", SqlDbType.Int).Value = plan.DestinationClientsModuleId;
            command.Parameters.Add("@import", SqlDbType.Int).Value = importModuleId;
            command.Parameters.Add("@identity", SqlDbType.NVarChar, 64).Value = identity;
            command.Parameters.Add("@tables", SqlDbType.Int).Value = tableCount;
            if (Convert.ToInt64(await command.ExecuteScalarAsync()) != 1)
                throw new ValidationException("Visit items require the completed original checkpoint with the same source, client scope, administrator and plan. Do not alter or delete it.");
        }

        private static async Task EnsureInventoryEmptyAsync(SqlConnection source, SqlConnection target, SqlTransaction transaction,
            IReadOnlyList<InventoryMap> selected, InventoryImportPlan plan)
        {
            foreach (var map in selected)
            {
                var scope = InventoryDestinationScope(map);
                var owner = map.Source == "GIBS_FBClientsVisitItem" ? plan.DestinationClientsModuleId : plan.DestinationInventoryModuleId;
                await using var sourceCommand = source.CreateCommand();
                sourceCommand.CommandTimeout = 120;
                sourceCommand.CommandText = $"SELECT x.{Quote(map.Key)} FROM dbo.{Quote(map.Source)} x WHERE {InventoryScope(map)} ORDER BY x.{Quote(map.Key)}";
                InventoryParameters(sourceCommand, plan);
                await using var targetCommand = target.CreateCommand();
                targetCommand.Transaction = transaction;
                targetCommand.CommandTimeout = 120;
                targetCommand.CommandText = $"SELECT x.{Quote(map.Key)}, {scope.Owner} FROM dbo.{Quote(map.Destination)} x WITH (UPDLOCK,HOLDLOCK){scope.Join} ORDER BY x.{Quote(map.Key)}";
                await using var sourceReader = await sourceCommand.ExecuteReaderAsync();
                await using var targetReader = await targetCommand.ExecuteReaderAsync();
                var hasSource = await sourceReader.ReadAsync();
                while (await targetReader.ReadAsync())
                {
                    if (targetReader.IsDBNull(1)) throw new ValidationException($"Ownership is unknown for a destination row in {map.Destination}.");
                    if (targetReader.GetInt32(1) == owner) throw new ValidationException($"The selected destination already contains rows in {map.Destination}. Review test data cleanup separately; no rows were deleted.");
                    var key = targetReader.GetInt32(0);
                    while (hasSource && sourceReader.GetInt32(0) < key) hasSource = await sourceReader.ReadAsync();
                    if (hasSource && sourceReader.GetInt32(0) == key) throw new ValidationException($"A source ID collides with another destination owner in {map.Destination}.");
                }
            }
        }

        private static async Task<ImportBatchCheckpoint.BatchResult> WriteInventoryBatchAsync(SqlConnection source, SqlConnection target,
            SqlTransaction transaction, InventoryMap map, InventoryImportPlan plan, string auditUser, int lastKey, bool missingLineItemsColumn, CancellationToken cancellationToken)
        {
            var integrity = await CheckDestinationIntegrityAsync(target, map.Destination, transaction);
            if (integrity.UnsafeConstraints > 0 || integrity.DisabledIndexes > 0 || integrity.DisabledTriggers > 0)
                throw new ValidationException($"Destination constraints, indexes, or triggers changed for {map.Destination}.");
            var fields = InventoryFields(map);
            await using var command = InventorySourceRows(source, map, plan, auditUser, missingLineItemsColumn, lastKey, RehearsalBatchSize);
            using var batch = new DataTable();
            var parents = new List<InventoryImportParentChecks.ParentReference>();
            var finalKey = lastKey;
            await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
            {
                for (var i = 0; i < fields.Count; i++) batch.Columns.Add(fields[i].Destination, reader.GetFieldType(i));
                var keyIndex = fields.FindIndex(field => field.Destination == map.Key);
                while (await reader.ReadAsync(cancellationToken))
                {
                    var id = reader.GetInt32(keyIndex);
                    if (id <= finalKey) throw new ValidationException("An inventory batch contains invalid or duplicate keys.");
                    finalKey = id;
                    var values = new object[fields.Count];
                    for (var i = 0; i < fields.Count; i++) values[i] = reader.GetValue(i);
                    batch.Rows.Add(values);
                    void Parent(string field, InventoryImportParentChecks.ParentKind kind, int? client = null)
                    {
                        var index = fields.FindIndex(mapping => mapping.Destination == field);
                        if (!reader.IsDBNull(index)) parents.Add(new(kind, reader.GetInt32(index), client));
                    }
                    switch (map.Source)
                    {
                        case "GIBS_FBProducts": Parent("ProductCategoryId", InventoryImportParentChecks.ParentKind.Category); break;
                        case "GIBS_FBInvoices": Parent("SupplierId", InventoryImportParentChecks.ParentKind.Supplier); break;
                        case "GIBS_FBLineItems":
                            Parent("InvoiceId", InventoryImportParentChecks.ParentKind.Invoice);
                            Parent("ProductId", InventoryImportParentChecks.ParentKind.Product);
                            break;
                        case "GIBS_FBClientsVisitItem":
                            Parent("VisitId", InventoryImportParentChecks.ParentKind.Visit, reader.GetInt32(fields.Count));
                            Parent("ProductId", InventoryImportParentChecks.ParentKind.Product);
                            break;
                    }
                }
            }
            if (batch.Rows.Count == 0) return new(lastKey, 0, true);
            await InventoryImportParentChecks.ValidateAsync(target, transaction, plan.DestinationInventoryModuleId,
                plan.DestinationClientsModuleId, plan.SourceClientsModuleId, parents, cancellationToken);
            using var bulk = new SqlBulkCopy(target, SqlBulkCopyOptions.KeepIdentity | SqlBulkCopyOptions.KeepNulls
                | SqlBulkCopyOptions.CheckConstraints | SqlBulkCopyOptions.FireTriggers, transaction);
            bulk.DestinationTableName = "dbo." + Quote(map.Destination);
            bulk.BulkCopyTimeout = 120;
            foreach (var field in fields) bulk.ColumnMappings.Add(field.Destination, field.Destination);
            await bulk.WriteToServerAsync(batch, cancellationToken);
            return new(finalKey, batch.Rows.Count, batch.Rows.Count < RehearsalBatchSize);
        }

        private static async Task ValidateExistingInventoryVisitsAsync(SqlConnection source, System.Data.Common.DbConnection target,
            System.Data.Common.DbTransaction transaction, InventoryImportPlan plan, CancellationToken cancellationToken)
        {
            await using var sourceCommand = source.CreateCommand();
            sourceCommand.CommandTimeout = 120;
            sourceCommand.CommandText = "SELECT DISTINCT v.VisitID,c.ClientID FROM dbo.GIBS_FBClientsVisitItem x JOIN dbo.GIBS_FBClientsVisits v ON v.VisitID=x.VisitID JOIN dbo.GIBS_FBClients c ON c.ClientID=v.ClientID WHERE c.ModuleID=@sourceClients";
            InventoryParameters(sourceCommand, plan);
            await using var reader = await sourceCommand.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                await using var check = target.CreateCommand();
                check.Transaction = transaction;
                check.CommandTimeout = 120;
                check.CommandText = "SELECT COUNT_BIG(*) FROM dbo.GIBSFBClientsVisits v JOIN dbo.GIBSFBClients c ON c.FBClientsId=v.FBClientsId WHERE v.VisitId=@visit AND v.LegacyVisitId=@visit AND c.FBClientsId=@client AND c.LegacyClientId=@client AND c.LegacyModuleId=@sourceClients AND c.ModuleId=@destinationClients";
                InventoryParameters(check, plan);
                var visit = check.CreateParameter(); visit.ParameterName = "@visit"; visit.Value = reader.GetInt32(0); check.Parameters.Add(visit);
                var client = check.CreateParameter(); client.ParameterName = "@client"; client.Value = reader.GetInt32(1); check.Parameters.Add(client);
                if (Convert.ToInt64(await check.ExecuteScalarAsync(cancellationToken)) != 1)
                    throw new ValidationException($"Imported visit {reader.GetInt32(0)} is missing or has a different client, owner, or legacy identity. Complete and reconcile the client/visit rehearsal first.");
            }
        }
    }
}
