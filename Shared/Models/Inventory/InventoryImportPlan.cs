using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace GIBS.Module.FBClients.ImportData
{
    public record InventoryImportPlan(string Source, int SourceInventoryModuleId, int SourceClientsModuleId,
        int DestinationInventoryModuleId, int DestinationClientsModuleId, List<string> Tables, string RunKey = "3inventory");

    public record InventoryImportTable(string Name, string Description, IReadOnlyList<string> Dependencies);
    public record InventoryImportReport(InventoryImportPlan Plan, bool Reconciliation,
        List<InventoryImportTableResult> Tables, List<ImportPreflightIssue> Issues);
    public record InventoryImportTableResult(ImportReconciliationTable Counts, List<ImportPreflightField> Fields, bool Compared = true);
    public record InventoryImportRunRequest(int MaxBatches);
    public record InventoryImportConfiguration(InventoryImportPlan Plan, bool RehearsalEnabled, int? RehearsalInventoryModuleId);

    public static class InventoryImportSelection
    {
        public const string PlanSetting = "ImportData:InventoryPlan";
        public const string RunKey = "3inventory";

        public static IReadOnlyList<InventoryImportTable> Tables { get; } = Array.AsReadOnly(new[]
        {
            new InventoryImportTable("GIBS_FBProductCategory", "Product categories", Array.Empty<string>()),
            new InventoryImportTable("GIBS_FBProducts", "Products", new[] { "GIBS_FBProductCategory" }),
            new InventoryImportTable("GIBS_FBSuppliers", "Suppliers", Array.Empty<string>()),
            new InventoryImportTable("GIBS_FBInvoices", "Invoices", new[] { "GIBS_FBSuppliers" }),
            new InventoryImportTable("GIBS_FBLineItems", "Invoice line items", new[] { "GIBS_FBProducts", "GIBS_FBInvoices" }),
            new InventoryImportTable("GIBS_FBClientsVisitItem", "Visit order items", new[] { "GIBS_FBProducts" })
        });

        public static InventoryImportPlan Normalize(InventoryImportPlan plan)
        {
            if (plan == null || string.IsNullOrWhiteSpace(plan.Source) || plan.SourceInventoryModuleId <= 0
                || plan.SourceClientsModuleId <= 0 || plan.DestinationInventoryModuleId <= 0
                || plan.DestinationClientsModuleId <= 0 || plan.Tables == null || plan.Tables.Count == 0
                || plan.Tables.Any(string.IsNullOrWhiteSpace)
                || plan.Tables.Distinct(StringComparer.Ordinal).Count() != plan.Tables.Count)
                throw new ValidationException("Specify the source, both source and destination module IDs, and a nonempty selection without duplicate tables.");

            var prefix = plan.RunKey == null ? "" : new string(plan.RunKey.TakeWhile(char.IsAsciiDigit).ToArray());
            if (plan.RunKey == null || plan.RunKey.Length > 128 || !int.TryParse(prefix, out var runNumber) || runNumber < 3
                || prefix.Length == plan.RunKey.Length || plan.RunKey.Any(c => !char.IsAsciiLetterOrDigit(c) && c != '-' && c != '_'))
                throw new ValidationException("Use a stable run key beginning with a number of at least 3 followed by a name, such as 3inventory.");
            return plan with { Tables = ExpandTables(plan.Tables) };
        }

        public static List<string> ExpandTables(IEnumerable<string> requested)
        {
            var known = Tables.ToDictionary(table => table.Name, StringComparer.Ordinal);
            var selected = new HashSet<string>(requested, StringComparer.Ordinal);
            if (selected.Any(name => name == null || !known.ContainsKey(name)))
                throw new ValidationException("The inventory selection contains an unsupported table.");
            var pending = new Stack<string>(selected);
            while (pending.Count > 0)
                foreach (var dependency in known[pending.Pop()].Dependencies)
                    if (selected.Add(dependency)) pending.Push(dependency);
            return Tables.Where(table => selected.Contains(table.Name)).Select(table => table.Name).ToList();
        }
    }
}
