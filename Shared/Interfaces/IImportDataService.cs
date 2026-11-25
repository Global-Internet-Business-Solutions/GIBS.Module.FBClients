using System.Collections.Generic;
using System.Threading.Tasks;

namespace GIBS.Module.FBClients.ImportData
{
    public static class ImportDataModule
    {
        public const string DefinitionName = "GIBS.Module.FBClients.ImportData, GIBS.Module.FBClients.Client.Oqtane";
        public const string PlanSetting = "ImportData:Plan";
        public const string ConnectionSetting = "ImportData:ProtectedConnection";
        public const string ModuleSource = "__module_connection__";
    }

    public interface IImportDataService
    {
        Task<bool> HasConnectionAsync(int moduleId);
        Task SaveConnectionAsync(int moduleId, ImportConnectionChange change);
        Task<ImportDataConfiguration> GetConfigurationAsync(int moduleId);
        Task<List<ImportSourceTable>> InspectAsync(int moduleId, string source);
        Task SavePlanAsync(int moduleId, ImportDataPlan plan);
        Task<ImportPreflightReport> PreflightAsync(int moduleId, ImportPreflightRequest request);
        Task<ImportReconciliationReport> ReconcileAsync(int moduleId, ImportPreflightRequest request);
        Task<ImportBatchRunReport> RunRehearsalBatchAsync(int moduleId, ImportRehearsalRequest request);
        Task<ImportBatchRunReport> RunTurkeyRehearsalBatchAsync(int moduleId, ImportRehearsalRequest request);
        Task<InventoryImportConfiguration> GetInventoryConfigurationAsync(int moduleId);
        Task<List<ImportSourceTable>> InspectInventoryAsync(int moduleId, string source);
        Task SaveInventoryPlanAsync(int moduleId, InventoryImportPlan plan);
        Task<InventoryImportReport> InventoryPreflightAsync(int moduleId);
        Task<InventoryImportReport> InventoryReconcileAsync(int moduleId);
        Task<ImportBatchRunReport> RunInventoryRehearsalAsync(int moduleId, InventoryImportRunRequest request);
    }

    public record ImportSource(string Id, string Name);
    public record ImportSourceTable(string Name, string Description, bool Available);
    public record ImportDataPlan(string Source, List<string> Tables, int? SourceModuleId = null);
    public record ImportConnectionChange(string ConnectionString, bool Remove);
    public record ImportDataConfiguration(List<ImportSource> Sources, ImportDataPlan Plan, bool RehearsalEnabled = false, int? RehearsalDestinationModuleId = null);
    public record ImportPreflightRequest(int DestinationModuleId, bool IncludeTurkeyRecipients = false);
    public record ImportRehearsalRequest(int DestinationModuleId, int MaxBatches);
    public record ImportBatchRunReport(long ImportedRows, int TablesCompleted, int TotalTables, bool Completed);
    public record ImportPreflightIssue(string Severity, string Table, string Message);
    public record ImportPreflightField(string Source, string Destination, string Rule, long? InvalidRows);
    public record ImportPreflightTable(string Source, string Destination, string SourceKey, string DestinationKey, long? SourceRows, long? Collisions, List<ImportPreflightField> Fields = null);
    public record ImportPreflightReport(string Source, int DestinationModuleId, List<ImportPreflightTable> Tables, List<ImportPreflightIssue> Issues, int? SourceModuleId = null);
    public record ImportReconciliationTable(string Source, string Destination, long SourceRows, long DestinationRows, long MatchedKeys, long MissingKeys, long ExtraKeys, long WrongOwner, long WrongParent, long WrongFamily, long ConflictingSourceFamily, long UnownedDestinationRows, long? SourceWideOrphans, long MappedValueMismatchRows = 0);
    public record ImportReconciliationReport(string Source, int SourceModuleId, int DestinationModuleId, List<ImportReconciliationTable> Tables);
    public sealed class ImportDataException(string message) : System.Exception(message);
}
