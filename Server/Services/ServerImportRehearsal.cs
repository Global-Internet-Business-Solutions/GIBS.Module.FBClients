using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
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
        private const int RehearsalBatchSize = 100;
        private const int RehearsalMaxBatches = 25;

        public Task<ImportBatchRunReport> RunRehearsalBatchAsync(int moduleId, ImportRehearsalRequest request) =>
            RunRehearsalAsync(moduleId, request, false);

        public Task<ImportBatchRunReport> RunTurkeyRehearsalBatchAsync(int moduleId, ImportRehearsalRequest request) =>
            RunRehearsalAsync(moduleId, request, true);

        private async Task<ImportBatchRunReport> RunRehearsalAsync(int moduleId, ImportRehearsalRequest request, bool turkeyOnly)
        {
            var (tenant, site) = await AuthorizeAsync(moduleId, edit: true);
            var enabled = bool.TryParse(configuration["FBClientsImport:Rehearsal:Enabled"], out var configuredEnabled) && configuredEnabled;
            var configuredDestination = int.TryParse(configuration["FBClientsImport:Rehearsal:DestinationModuleId"], out var destinationId)
                ? destinationId : 0;
            if (!enabled || configuredDestination <= 0)
                throw new ValidationException("The host has not enabled a rehearsal destination.");
            if (request == null || request.DestinationModuleId != configuredDestination
                || request.MaxBatches is < 1 or > RehearsalMaxBatches)
                throw new ValidationException($"Choose the host-configured rehearsal destination and between 1 and {RehearsalMaxBatches} batches.");
            var destination = modules.GetModule(configuredDestination);
            if (destination == null || destination.IsDeleted || destination.SiteId != site
                || destination.ModuleDefinitionName != FulfillmentModule.ClientsDefinitionName)
                throw new ValidationException("The configured rehearsal destination is not an FBClients module on this site.");

            var plan = (await GetConfigurationAsync(moduleId)).Plan;
            if (plan == null || string.IsNullOrWhiteSpace(plan.Source) || plan.SourceModuleId is null or <= 0
                || plan.Tables == null || plan.Tables.Count == 0 || plan.Tables.Distinct(StringComparer.Ordinal).Count() != plan.Tables.Count
                || plan.Tables.Any(name => !Mappings.Any(map => map.Source == name && !name.EndsWith("_Merged", StringComparison.Ordinal))))
                throw new ValidationException("Save a valid source, source ModuleID, and active-table selection before running the rehearsal. Merged tables are not supported.");
            if (plan.Tables.Any(name => name.EndsWith("_Merged", StringComparison.Ordinal)))
                throw new ValidationException("Merged tables are not supported in the first rehearsal.");
            if (turkeyOnly && (!plan.Tables.Contains("GIBS_FBClients", StringComparer.Ordinal)
                || plan.Tables.Contains("GIBS_FBTurkeyRecipients", StringComparer.Ordinal)))
                throw new ValidationException("Keep the original saved selection, including clients but excluding turkey recipients, for the supplementary rehearsal.");

            var user = http.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true && authentication != null)
                user = (await authentication.GetAuthenticationStateAsync()).User;
            var auditUser = user?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(auditUser) || auditUser.Length > 256)
                throw new ValidationException("The authenticated administrator identity is unavailable or too long to own this rehearsal.");

            var connectionString = ResolveSourceConnection(tenant, site, plan.Source, moduleId);
            var operation = "opening the read-only source snapshot";
            try
            {
                var builder = new SqlConnectionStringBuilder(connectionString)
                {
                    ApplicationName = "FBClients Import Rehearsal",
                    ApplicationIntent = ApplicationIntent.ReadOnly
                };
                if (string.IsNullOrWhiteSpace(builder.InitialCatalog))
                    throw new ValidationException("The DNN source must specify a database.");
                await using var source = new SqlConnection(builder.ConnectionString);
                await source.OpenAsync(http.HttpContext?.RequestAborted ?? CancellationToken.None);
                await using var db = factory.CreateDbContext();
                if (!db.Database.IsSqlServer())
                    throw new ValidationException("The import rehearsal currently supports SQL Server destinations only.");
                await db.Database.OpenConnectionAsync(http.HttpContext?.RequestAborted ?? CancellationToken.None);
                if (db.Database.GetDbConnection() is not SqlConnection target)
                    throw new ValidationException("The configured rehearsal destination is not a SQL Server connection.");

                var cancellationToken = http.HttpContext?.RequestAborted ?? CancellationToken.None;
                var azureDatabase = configuration["FBClientsImport:Rehearsal:FrozenAzureDatabase"];
                Func<Task<ImportBatchCheckpoint.SourceSnapshot>> readSourceIdentity = null;
                if (!string.IsNullOrWhiteSpace(azureDatabase))
                {
                    if (!bool.TryParse(configuration["FBClientsImport:Rehearsal:AzureCopyFreezeConfirmed"], out var frozen) || !frozen)
                        throw new ValidationException("A host administrator must confirm the Azure copy is frozen against all other writers before rehearsal.");
                    var fingerprintTables = Mappings.Where(map => plan.Tables.Contains(map.Source, StringComparer.Ordinal)
                        || map.Source == "GIBS_FBClients"
                        || (plan.Tables.Contains("GIBS_FBxMas", StringComparer.Ordinal) && map.Source == "GIBS_FBClientsAddFamMem"))
                        .Select(map => (Table: map.Source, Key: map.SourceKey)).ToList();
                    if (plan.Tables.Contains("GIBS_FBxMas", StringComparer.Ordinal))
                        fingerprintTables.Add(("GIBS_FBClientsAddFamMem_Merged", "ClAddFamMemID"));
                    readSourceIdentity = () => ImportAzureFrozenSource.ReadAsync(source, azureDatabase, fingerprintTables, cancellationToken);
                }
                string requiredCompletedIdentity = null;
                var selectedTables = plan.Tables;
                if (turkeyOnly)
                {
                    var originalTables = Mappings.Where(map => plan.Tables.Contains(map.Source, StringComparer.Ordinal)).ToArray();
                    var originalSourceIdentity = readSourceIdentity == null
                        ? await ImportBatchCheckpoint.ReadSnapshotAsync(source) : await readSourceIdentity();
                    requiredCompletedIdentity = ImportBatchCheckpoint.Identity(originalSourceIdentity, moduleId, destination.ModuleId,
                        plan.SourceModuleId.Value, originalTables.Select(map => map.Source).ToArray(),
                        BatchPlanIdentity(originalTables, auditUser, RehearsalBatchSize));
                    selectedTables = ["GIBS_FBTurkeyRecipients"];
                    if (!string.IsNullOrWhiteSpace(azureDatabase))
                    {
                        var fingerprintTables = Mappings.Where(map => plan.Tables.Contains(map.Source, StringComparer.Ordinal)
                            || map.Source == "GIBS_FBClients" || map.Source == "GIBS_FBTurkeyRecipients"
                            || (plan.Tables.Contains("GIBS_FBxMas", StringComparer.Ordinal) && map.Source == "GIBS_FBClientsAddFamMem"))
                            .Select(map => (Table: map.Source, Key: map.SourceKey)).ToList();
                        if (plan.Tables.Contains("GIBS_FBxMas", StringComparer.Ordinal))
                            fingerprintTables.Add(("GIBS_FBClientsAddFamMem_Merged", "ClAddFamMemID"));
                        var readOriginalIdentity = readSourceIdentity;
                        readSourceIdentity = async () =>
                        {
                            if (await readOriginalIdentity() != originalSourceIdentity)
                                throw new ValidationException("The original frozen source changed after its completed rehearsal; the turkey batch was not committed.");
                            return await ImportAzureFrozenSource.ReadAsync(source, azureDatabase, fingerprintTables, cancellationToken);
                        };
                    }
                }
                operation = "validating the source and writing bounded rehearsal batches";
                var progress = await WriteImportBatchesAsync(source, target, moduleId, destination.ModuleId,
                    plan.SourceModuleId.Value, selectedTables, auditUser, RehearsalBatchSize, request.MaxBatches,
                    cancellationToken, readSourceIdentity, requiredCompletedIdentity, turkeyOnly ? plan.Tables.Count : 0,
                    turkeyOnly ? "2supplemental" : null);
                return new(progress.ImportedRows, progress.TableIndex, selectedTables.Count, progress.Completed);
            }
            catch (SqlException ex)
            {
                logger.Log(LogLevel.Error, this, LogFunction.Other,
                    "Import rehearsal failed for module {ModuleId}, destination {DestinationModuleId}, while {Operation}: SQL error {SqlNumber}, state {SqlState}, procedure {Procedure}, line {LineNumber}.",
                    moduleId, configuredDestination, operation, ex.Number, ex.State, ex.Procedure ?? "", ex.LineNumber);
                foreach (SqlError error in ex.Errors)
                {
                    if (error.Number == 208)
                        logger.Log(LogLevel.Error, this, LogFunction.Other,
                            "Import rehearsal missing SQL object for module {ModuleId}, destination {DestinationModuleId}: {ObjectError}; procedure {Procedure}, line {LineNumber}.",
                            moduleId, configuredDestination, error.Message, error.Procedure ?? "", error.LineNumber);
                }
                throw new ImportDataException("The rehearsal could not complete. Check the server logs for the SQL error number, missing object name, and location; committed batches remain checkpointed and can be resumed with the same administrator and plan.");
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
            {
                logger.Log(LogLevel.Error, this, LogFunction.Other,
                    "Import rehearsal failed for module {ModuleId}, destination {DestinationModuleId}, while {Operation}: {ErrorType}.",
                    moduleId, configuredDestination, operation, ex.GetType().Name);
                throw new ImportDataException("The rehearsal could not complete. Check the server logs and verify the source snapshot, destination schema, and checkpoint migration.");
            }
        }
    }
}
