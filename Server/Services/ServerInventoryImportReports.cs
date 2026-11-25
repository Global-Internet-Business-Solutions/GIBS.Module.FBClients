using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace GIBS.Module.FBClients.ImportData
{
    public partial class ServerImportDataService
    {
        public Task<InventoryImportReport> InventoryPreflightAsync(int moduleId) => InventoryReportAsync(moduleId, false);
        public Task<InventoryImportReport> InventoryReconcileAsync(int moduleId) => InventoryReportAsync(moduleId, true);

        private async Task<InventoryImportReport> InventoryReportAsync(int moduleId, bool reconciliation)
        {
            var (tenant, site) = await AuthorizeAsync(moduleId);
            var plan = ReadInventoryPlan(moduleId);
            ValidateInventoryDestinations(plan, site, false);
            return await InventoryDatabaseOperationAsync(moduleId, async () =>
            {
                await using var source = new SqlConnection(InventorySourceConnection(tenant, site, plan.Source, moduleId));
                await source.OpenAsync();
                await using var db = factory.CreateDbContext();
                if (!db.Database.IsSqlServer()) throw new ValidationException("Inventory import supports SQL Server destinations only.");
                await db.Database.OpenConnectionAsync();
                var target = db.Database.GetDbConnection();
                var issues = new List<ImportPreflightIssue>
                {
                    new("Warning", "", "Read-only point-in-time report, not production import approval or proof of provenance/trigger effects. Rehearsal requires a host-configured isolated destination and the same frozen source. Original client and turkey plans are unchanged."),
                    new("Warning", "", "Only selected tables and automatically included inventory dependencies are compared. Visit items use the selected client scope; other inventory uses the selected inventory scope. Zero-row tables are supported; skipped tables are not verified.")
                };
                var tables = new List<InventoryImportTableResult>();
                foreach (var map in InventoryMappings.Where(map => plan.Tables.Contains(map.Source, StringComparer.Ordinal)))
                {
                    var start = issues.Count;
                    var orphans = await ValidateInventorySourceAsync(source, map, plan, issues);
                    if (!await IsIdentityPrimaryKeyAsync(target, map.Destination, map.Key))
                        throw new ValidationException($"Destination {map.Destination} requires a single-column identity primary key.");
                    var integrity = await CheckDestinationIntegrityAsync(target, map.Destination);
                    if (integrity.UnsafeConstraints > 0 || integrity.DisabledIndexes > 0 || integrity.DisabledTriggers > 0)
                        issues.Add(new("Blocker", map.Source, "Destination has disabled or untrusted constraints, disabled unique indexes, or disabled triggers."));
                    var fields = await ValidateInventoryFieldsAsync(source, target, map, plan, issues);
                    if (issues.Skip(start).Any(issue => issue.Severity == "Blocker"))
                    {
                        tables.Add(new(new(map.Source, map.Destination, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, orphans), fields, false));
                        continue;
                    }
                    var counts = await CompareInventoryAsync(source, target, map, plan, orphans);
                    tables.Add(new(counts, fields));
                    if (!reconciliation && counts.DestinationRows > 0)
                        issues.Add(new("Blocker", map.Source, $"The selected destination already has {counts.DestinationRows:N0} rows. Fresh selected destination tables are required; no cleanup or truncation is performed."));
                    if (!reconciliation && counts.MatchedKeys + counts.WrongOwner > 0)
                        issues.Add(new("Blocker", map.Source, "Scoped source IDs already exist in the destination. ID collisions are checked tenant-wide."));
                    if (counts.UnownedDestinationRows > 0)
                        issues.Add(new("Blocker", map.Source, "Destination rows have unknown module ownership and require review."));
                }
                if (plan.Tables.Contains("GIBS_FBClientsVisitItem", StringComparer.Ordinal)
                    && !issues.Any(issue => issue.Table == "GIBS_FBClientsVisitItem" && issue.Severity == "Blocker"))
                    await ValidateExistingInventoryVisitsAsync(source, target, null, plan, default);
                return new InventoryImportReport(plan, reconciliation, tables, issues);
            });
        }

        private static SqlCommand InventorySourceRows(SqlConnection source, InventoryMap map, InventoryImportPlan plan, string auditUser,
            bool missingLineItemsColumn, int? lastKey = null, int batchSize = 100)
        {
            var fields = InventoryFields(map);
            var command = source.CreateCommand();
            command.CommandTimeout = 120;
            var client = map.Source == "GIBS_FBClientsVisitItem"
                ? "(SELECT v.ClientID FROM dbo.GIBS_FBClientsVisits v WHERE v.VisitID=x.VisitID)" : "CAST(NULL AS int)";
            command.CommandText = $"SELECT {(lastKey.HasValue ? "TOP (@batchSize) " : "")}{string.Join(",", fields.Select(field => InventoryValue(map, field, missingLineItemsColumn) + " AS " + Quote(field.Destination)))}, {client} AS [__client] FROM dbo.{Quote(map.Source)} x WHERE {InventoryScope(map)}{(lastKey.HasValue ? " AND x." + Quote(map.Key) + ">@lastKey" : "")} ORDER BY x.{Quote(map.Key)}";
            InventoryParameters(command, plan);
            command.Parameters.Add("@auditUser", SqlDbType.NVarChar, 256).Value = auditUser;
            command.Parameters.Add("@timestamp", SqlDbType.DateTime2).Value = DateTime.UtcNow;
            if (lastKey.HasValue)
            {
                command.Parameters.Add("@lastKey", SqlDbType.Int).Value = lastKey.Value;
                command.Parameters.Add("@batchSize", SqlDbType.Int).Value = batchSize;
            }
            return command;
        }

        private static async Task<ImportReconciliationTable> CompareInventoryAsync(SqlConnection source, DbConnection target,
            InventoryMap map, InventoryImportPlan plan, long orphans)
        {
            var fields = InventoryFields(map);
            var scope = InventoryDestinationScope(map);
            var missingLineItemsColumn = await MissingInventoryLineItemsColumnAsync(source, map);
            await using var sourceCommand = InventorySourceRows(source, map, plan, "report", missingLineItemsColumn);
            await using var targetCommand = target.CreateCommand();
            targetCommand.CommandTimeout = 120;
            targetCommand.CommandText = $"SELECT {string.Join(",", fields.Select(field => "x." + Quote(field.Destination)))}, {scope.Client} AS [__client], {scope.Owner} AS [__owner] FROM dbo.{Quote(map.Destination)} x{scope.Join} ORDER BY x.{Quote(map.Key)}";
            await using var sourceReader = await sourceCommand.ExecuteReaderAsync();
            await using var targetReader = await targetCommand.ExecuteReaderAsync();
            var sourceKey = fields.FindIndex(field => field.Destination == map.Key);
            var ownerId = map.Source == "GIBS_FBClientsVisitItem" ? plan.DestinationClientsModuleId : plan.DestinationInventoryModuleId;
            long sourceRows = 0, destinationRows = 0, matched = 0, missing = 0, extra = 0, wrongOwner = 0, wrongParent = 0, unowned = 0, mismatches = 0;
            var hasSource = await sourceReader.ReadAsync();
            var hasTarget = await targetReader.ReadAsync();
            while (hasSource || hasTarget)
            {
                if (!hasTarget || hasSource && sourceReader.GetInt32(sourceKey) < targetReader.GetInt32(sourceKey))
                {
                    sourceRows++; missing++;
                    hasSource = await sourceReader.ReadAsync();
                    continue;
                }
                var owns = !targetReader.IsDBNull(fields.Count + 1) && targetReader.GetInt32(fields.Count + 1) == ownerId;
                if (owns) destinationRows++;
                if (targetReader.IsDBNull(fields.Count + 1)) unowned++;
                if (!hasSource || targetReader.GetInt32(sourceKey) < sourceReader.GetInt32(sourceKey))
                {
                    if (owns) extra++;
                    hasTarget = await targetReader.ReadAsync();
                    continue;
                }
                sourceRows++;
                if (owns) matched++; else wrongOwner++;
                var mismatch = false;
                var parentMismatch = !InventoryEqual(sourceReader.GetValue(fields.Count), targetReader.GetValue(fields.Count));
                for (var i = 0; i < fields.Count; i++)
                {
                    if (fields[i].Source == null) continue;
                    if (InventoryEqual(sourceReader.GetValue(i), targetReader.GetValue(i))) continue;
                    mismatch = true;
                    if (fields[i].Destination != map.Key && fields[i].Destination is "ProductCategoryId" or "ProductId" or "SupplierId" or "InvoiceId" or "VisitId") parentMismatch = true;
                }
                var createdBy = fields.FindIndex(field => field.Destination == "CreatedBy");
                var modifiedBy = fields.FindIndex(field => field.Destination == "ModifiedBy");
                var createdOn = fields.FindIndex(field => field.Destination == "CreatedOn");
                var modifiedOn = fields.FindIndex(field => field.Destination == "ModifiedOn");
                if (createdBy < 0 || modifiedBy < 0 || createdOn < 0 || modifiedOn < 0)
                    throw new InvalidOperationException("Inventory audit mapping is incomplete.");
                if (targetReader.IsDBNull(createdBy) || string.IsNullOrWhiteSpace(targetReader.GetString(createdBy))
                    || !InventoryEqual(targetReader.GetValue(createdBy), targetReader.GetValue(modifiedBy))
                    || targetReader.IsDBNull(createdOn) || !InventoryEqual(targetReader.GetValue(createdOn), targetReader.GetValue(modifiedOn))) mismatch = true;
                if (mismatch) mismatches++;
                if (parentMismatch) wrongParent++;
                hasSource = await sourceReader.ReadAsync();
                hasTarget = await targetReader.ReadAsync();
            }
            return new(map.Source, map.Destination, sourceRows, destinationRows, matched, missing, extra, wrongOwner, wrongParent, 0, 0, unowned, orphans, mismatches);
        }

        private static bool InventoryEqual(object source, object destination)
        {
            if (source is byte[] first && destination is byte[] second) return first.AsSpan().SequenceEqual(second);
            return Equals(source, destination);
        }
    }
}
