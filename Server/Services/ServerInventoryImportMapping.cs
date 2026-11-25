using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using GIBS.Module.FBClients.Inventory.Models;
using GIBS.Module.FBClients.Models;
using Microsoft.Data.SqlClient;

namespace GIBS.Module.FBClients.ImportData
{
    public partial class ServerImportDataService
    {
        private record InventoryMap(string Source, string Destination, string Key, string LegacyKey, Type Model);
        private static readonly InventoryMap[] InventoryMappings =
        [
            new("GIBS_FBProductCategory", "GIBSFBProductCategory", "ProductCategoryId", "LegacyProductCategoryId", typeof(FBProductCategory)),
            new("GIBS_FBProducts", "GIBSFBProducts", "ProductId", "LegacyProductId", typeof(FBProduct)),
            new("GIBS_FBSuppliers", "GIBSFBSuppliers", "SupplierId", "LegacySupplierId", typeof(FBSupplier)),
            new("GIBS_FBInvoices", "GIBSFBInvoices", "InvoiceId", "LegacyInvoiceId", typeof(FBInvoice)),
            new("GIBS_FBLineItems", "GIBSFBLineItems", "LineItemId", "LegacyLineItemId", typeof(FBLineItem)),
            new("GIBS_FBClientsVisitItem", "GIBSFBClientsVisitItem", "VisitItemId", "LegacyVisitItemId", typeof(FBClientsVisitItem))
        ];

        private static List<FieldMap> InventoryFields(InventoryMap map) => map.Model.GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Select(property =>
            {
                var name = property.Name;
                if (name is "CreatedBy" or "ModifiedBy") return new FieldMap(name, null, "Authenticated importing administrator; not a DNN audit user.");
                if (name is "CreatedOn" or "ModifiedOn") return new FieldMap(name, null, "UTC import timestamp; original inventory audit dates remain in legacy fields.");
                if (name == "ModuleId") return new FieldMap(name, null, "Selected destination Inventory module; source ModuleID is bound to the saved run identity.");
                if (name == map.Key || name == map.LegacyKey) return new FieldMap(name, map.Key, "Preserve source ID; block tenant-wide ID collisions.");
                if (name == "ProductCategoryId" && map.Source == "GIBS_FBProducts") return new FieldMap(name, name, "Preserve category ID; map source 0 to NULL for uncategorized products. Positive IDs require the selected inventory parent.");
                var source = name switch
                {
                    "LegacyPortalId" => "PortalID",
                    "LegacyCreatedOnDate" => "CreatedOnDate",
                    "LegacyCreatedByUserId" => "CreatedByUserID",
                    "LegacyLastModifiedOnDate" => "LastModifiedOnDate",
                    "LegacyLastModifiedByUserId" => "LastModifiedByUserID",
                    "GIBSFBLineItems" => "GIBS_FBLineItems",
                    _ => name
                };
                return new FieldMap(name, source, "Copy without trimming or null substitution; preserve validated parent IDs and historical values.");
            }).ToList();

        private static string InventoryScope(InventoryMap map) => map.Source switch
        {
            "GIBS_FBLineItems" => "EXISTS (SELECT 1 FROM dbo.GIBS_FBInvoices i WHERE i.InvoiceID=x.InvoiceID AND i.ModuleID=@sourceInventory)",
            "GIBS_FBClientsVisitItem" => "EXISTS (SELECT 1 FROM dbo.GIBS_FBClientsVisits v JOIN dbo.GIBS_FBClients c ON c.ClientID=v.ClientID WHERE v.VisitID=x.VisitID AND c.ModuleID=@sourceClients)",
            _ => "x.ModuleID=@sourceInventory"
        };

        private static (string Join, string Owner, string Client) InventoryDestinationScope(InventoryMap map) => map.Source switch
        {
            "GIBS_FBLineItems" => (" LEFT JOIN dbo.GIBSFBInvoices i ON i.InvoiceId=x.InvoiceId", "i.ModuleId", "NULL"),
            "GIBS_FBClientsVisitItem" => (" LEFT JOIN dbo.GIBSFBClientsVisits v ON v.VisitId=x.VisitId LEFT JOIN dbo.GIBSFBClients c ON c.FBClientsId=v.FBClientsId", "c.ModuleId", "v.FBClientsId"),
            _ => ("", "x.ModuleId", "NULL")
        };

        private static string InventoryValue(InventoryMap map, FieldMap field, bool missingLineItemsColumn) => field.Source == null
            ? field.Destination switch
            {
                "ModuleId" => "@destinationInventory",
                "CreatedBy" or "ModifiedBy" => "@auditUser",
                "CreatedOn" or "ModifiedOn" => "@timestamp",
                _ => throw new InvalidOperationException("Unsupported generated inventory field.")
            }
            : missingLineItemsColumn && map.Source == "GIBS_FBLineItems" && field.Destination == "GIBSFBLineItems"
                ? "CAST(NULL AS decimal(18,2))"
                : map.Source == "GIBS_FBProducts" && field.Destination == "ProductCategoryId"
                    ? "NULLIF(x.ProductCategoryID,0)" : "x." + Quote(field.Source);

        private static async Task<bool> MissingInventoryLineItemsColumnAsync(DbConnection source, InventoryMap map)
        {
            if (map.Source != "GIBS_FBLineItems") return false;
            var columns = await ColumnShapesAsync(source, map.Source);
            if (columns.ContainsKey("GIBS_FBLineItems")) return false;
            await using var permissions = source.CreateCommand();
            permissions.CommandTimeout = 120;
            permissions.CommandText = "SELECT HAS_PERMS_BY_NAME('dbo.GIBS_FBLineItems', 'OBJECT', 'SELECT')";
            var permitted = await permissions.ExecuteScalarAsync();
            if (permitted == null || permitted == DBNull.Value || Convert.ToInt32(permitted) != 1)
                throw new ValidationException("Table-level SELECT permission is required to confirm whether the source line-item column is absent.");
            await using var command = source.CreateCommand();
            command.CommandTimeout = 120;
            command.CommandText = "SELECT TOP (0) [GIBS_FBLineItems] FROM [dbo].[GIBS_FBLineItems]";
            try
            {
                await using var reader = await command.ExecuteReaderAsync();
                throw new ValidationException("Source line-item column exists but its metadata is not visible; verify source permissions before import.");
            }
            catch (SqlException ex) when (ex.Number == 207)
            {
                return true;
            }
        }

        private static void InventoryParameters(DbCommand command, InventoryImportPlan plan)
        {
            foreach (var pair in new[] { ("@sourceInventory", plan.SourceInventoryModuleId), ("@sourceClients", plan.SourceClientsModuleId),
                ("@destinationInventory", plan.DestinationInventoryModuleId), ("@destinationClients", plan.DestinationClientsModuleId) })
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = pair.Item1;
                parameter.Value = pair.Item2;
                command.Parameters.Add(parameter);
            }
        }

        private static async Task<long> InventoryCountAsync(DbConnection connection, string sql, InventoryImportPlan plan, DbTransaction transaction = null)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandTimeout = 120;
            command.CommandText = sql;
            InventoryParameters(command, plan);
            return Convert.ToInt64(await command.ExecuteScalarAsync());
        }

        private static async Task<List<ImportPreflightField>> ValidateInventoryFieldsAsync(DbConnection source, DbConnection target,
            InventoryMap map, InventoryImportPlan plan, List<ImportPreflightIssue> issues)
        {
            var fields = InventoryFields(map);
            var sourceColumns = await ColumnShapesAsync(source, map.Source);
            var targetColumns = await ColumnShapesAsync(target, map.Destination);
            var results = new List<ImportPreflightField>();
            foreach (var field in fields)
            {
                if (!targetColumns.TryGetValue(field.Destination, out var destination))
                {
                    issues.Add(new("Blocker", map.Source, $"Destination column {field.Destination} is missing or not visible."));
                    results.Add(new(field.Source ?? "Generated", field.Destination, field.Rule, null));
                    continue;
                }
                if (field.Source == null) { results.Add(new("Generated", field.Destination, field.Rule, null)); continue; }
                if (!sourceColumns.TryGetValue(field.Source, out var origin))
                {
                    if (map.Source == "GIBS_FBLineItems" && field.Destination == "GIBSFBLineItems"
                        && destination.Nullable && (destination.Type is "decimal" or "numeric")
                        && await MissingInventoryLineItemsColumnAsync(source, map))
                    {
                        const string rule = "Source GIBS_FBLineItems column is confirmed absent: generate NULL under the approved policy. Do not infer a value; existing source values are preserved when the column exists.";
                        issues.Add(new("Warning", map.Source, rule));
                        results.Add(new("Generated: NULL", field.Destination, rule, 0));
                        continue;
                    }
                    issues.Add(new("Blocker", map.Source, $"Source column {field.Source} is missing or incompatible with {field.Destination}."));
                    results.Add(new(field.Source, field.Destination, field.Rule, null));
                    continue;
                }
                if (!CompatibleType(origin.Type, destination.Type))
                {
                    issues.Add(new("Blocker", map.Source, $"Source column {field.Source} is missing or incompatible with {field.Destination}."));
                    results.Add(new(field.Source, field.Destination, field.Rule, null));
                    continue;
                }
                var predicate = ValidationPredicate(field, destination, origin);
                var invalid = await InventoryCountAsync(source, $"SELECT COUNT_BIG(*) FROM dbo.{Quote(map.Source)} x WHERE {InventoryScope(map)} AND ({predicate})", plan);
                results.Add(new(field.Source, field.Destination, field.Rule, invalid));
                if (invalid > 0) issues.Add(new("Blocker", map.Source, $"{invalid:N0} scoped values cannot map {field.Source} without null, length, encoding, range or precision loss."));
            }
            foreach (var name in sourceColumns.Keys.Where(name => !fields.Any(field => string.Equals(field.Source, name, StringComparison.OrdinalIgnoreCase))))
            {
                if (name.Equals("ModuleID", StringComparison.OrdinalIgnoreCase) && map.Source is not "GIBS_FBLineItems" and not "GIBS_FBClientsVisitItem")
                    results.Add(new(name, "Run identity", "Source inventory ModuleID scopes the run and is retained in its immutable checkpoint identity; it is not copied into Oqtane ModuleId.", 0));
                else issues.Add(new("Blocker", map.Source, $"Source column {name} has no approved mapping; it will not be silently discarded."));
            }
            foreach (var name in targetColumns.Keys.Where(name => !fields.Any(field => string.Equals(field.Destination, name, StringComparison.OrdinalIgnoreCase))))
                issues.Add(new("Blocker", map.Source, $"Destination column {name} has no mapping or generation rule."));
            return results;
        }

        private static async Task<long> ValidateInventorySourceAsync(DbConnection source, InventoryMap map, InventoryImportPlan plan,
            List<ImportPreflightIssue> issues)
        {
            var columns = await ColumnsAsync(source, map.Source);
            if (!HasInt(columns, map.Key)) throw new ValidationException($"Integer key metadata for {map.Source} is missing.");
            var parents = map.Source switch
            {
                "GIBS_FBProducts" => new[] { ("ProductCategoryID", "GIBS_FBProductCategory", "ProductCategoryID") },
                "GIBS_FBInvoices" => new[] { ("SupplierID", "GIBS_FBSuppliers", "SupplierID") },
                "GIBS_FBLineItems" => new[] { ("InvoiceID", "GIBS_FBInvoices", "InvoiceID"), ("ProductID", "GIBS_FBProducts", "ProductID") },
                "GIBS_FBClientsVisitItem" => new[] { ("VisitID", "GIBS_FBClientsVisits", "VisitID"), ("ProductID", "GIBS_FBProducts", "ProductID") },
                _ => Array.Empty<(string, string, string)>()
            };
            foreach (var (column, table, key) in parents)
                if (!HasInt(columns, column) || !HasInt(await ColumnsAsync(source, table), key))
                    throw new ValidationException($"Parent metadata {map.Source}.{column} -> {table}.{key} is missing.");
            if (map.Source == "GIBS_FBClientsVisitItem")
            {
                var clients = await ColumnsAsync(source, "GIBS_FBClients");
                var visits = await ColumnsAsync(source, "GIBS_FBClientsVisits");
                if (!HasInt(clients, "ClientID") || !HasInt(clients, "ModuleID") || !HasInt(visits, "ClientID"))
                    throw new ValidationException("Source visit/client scope metadata is missing.");
                var ambiguous = await InventoryCountAsync(source, "SELECT COUNT_BIG(*) FROM dbo.GIBS_FBClientsVisitItem x WHERE EXISTS (SELECT 1 FROM dbo.GIBS_FBClientsVisits v JOIN dbo.GIBS_FBClients c ON c.ClientID=v.ClientID WHERE v.VisitID=x.VisitID AND c.ModuleID=@sourceClients AND (v.VisitID<=0 OR c.ClientID<=0 OR (SELECT COUNT_BIG(*) FROM dbo.GIBS_FBClientsVisits av WHERE av.VisitID=v.VisitID)<>1 OR (SELECT COUNT_BIG(*) FROM dbo.GIBS_FBClients ac WHERE ac.ClientID=c.ClientID)<>1))", plan);
                if (ambiguous > 0) issues.Add(new("Blocker", map.Source, $"{ambiguous:N0} scoped visit items have ambiguous or invalid source visit/client identities."));
            }
            else if (map.Source != "GIBS_FBLineItems")
            {
                if (!HasInt(columns, "ModuleID") || !HasInt(columns, "PortalID")) throw new ValidationException($"Module and portal metadata for {map.Source} is missing.");
                var portals = await InventoryCountAsync(source, $"SELECT COUNT_BIG(*) FROM (SELECT DISTINCT PortalID FROM dbo.{Quote(map.Source)} x WHERE {InventoryScope(map)}) p", plan);
                if (portals > 1) issues.Add(new("Blocker", map.Source, "The selected inventory module spans multiple portals."));
            }
            var invalid = await InventoryCountAsync(source, $"SELECT COUNT_BIG(*) FROM (SELECT x.{Quote(map.Key)} FROM dbo.{Quote(map.Source)} x WHERE {InventoryScope(map)} GROUP BY x.{Quote(map.Key)} HAVING x.{Quote(map.Key)} IS NULL OR x.{Quote(map.Key)}<=0 OR COUNT_BIG(*)>1 OR (SELECT COUNT_BIG(*) FROM dbo.{Quote(map.Source)} allRows WHERE allRows.{Quote(map.Key)}=x.{Quote(map.Key)})>1) bad", plan);
            if (invalid > 0) issues.Add(new("Blocker", map.Source, $"{invalid:N0} scoped keys are null, nonpositive, or duplicated anywhere in the source table."));
            foreach (var (column, table, key) in parents.Where(parent => parent.Item1 is not "VisitID" and not "InvoiceID"))
            {
                var optional = column == "SupplierID" ? $"x.{Quote(column)} IS NOT NULL" : column == "ProductCategoryID" ? "x.ProductCategoryID IS NOT NULL AND x.ProductCategoryID<>0" : "1=1";
                var missing = await InventoryCountAsync(source, $"SELECT COUNT_BIG(*) FROM dbo.{Quote(map.Source)} x WHERE {InventoryScope(map)} AND {optional} AND NOT EXISTS (SELECT 1 FROM dbo.{Quote(table)} p WHERE p.{Quote(key)}=x.{Quote(column)} AND p.ModuleID=@sourceInventory)", plan);
                if (missing > 0) issues.Add(new("Blocker", map.Source, $"{missing:N0} scoped rows reference a missing or different-module {table} parent. No rows are approved for exclusion."));
            }
            var orphanSql = map.Source switch
            {
                "GIBS_FBLineItems" => "SELECT COUNT_BIG(*) FROM dbo.GIBS_FBLineItems x WHERE NOT EXISTS (SELECT 1 FROM dbo.GIBS_FBInvoices p WHERE p.InvoiceID=x.InvoiceID)",
                "GIBS_FBClientsVisitItem" => "SELECT COUNT_BIG(*) FROM dbo.GIBS_FBClientsVisitItem x WHERE NOT EXISTS (SELECT 1 FROM dbo.GIBS_FBClientsVisits v JOIN dbo.GIBS_FBClients c ON c.ClientID=v.ClientID WHERE v.VisitID=x.VisitID)",
                _ => null
            };
            var orphans = orphanSql == null ? 0 : await InventoryCountAsync(source, orphanSql, plan);
            if (orphans > 0) issues.Add(new("Blocker", map.Source, $"{orphans:N0} source-wide parentless rows cannot be scoped and require review. They are not approved exclusions."));
            return orphans;
        }
    }
}
