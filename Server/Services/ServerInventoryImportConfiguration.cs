using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using GIBS.Module.FBClients.Fulfillment;
using GIBS.Module.FBClients.Inventory;
using Microsoft.Data.SqlClient;
using Oqtane.Enums;
using Oqtane.Models;
using Oqtane.Shared;

namespace GIBS.Module.FBClients.ImportData
{
    public partial class ServerImportDataService
    {
        public async Task<InventoryImportConfiguration> GetInventoryConfigurationAsync(int moduleId)
        {
            await AuthorizeAsync(moduleId);
            var enabled = bool.TryParse(configuration["FBClientsImport:Rehearsal:Enabled"], out var value) && value;
            int? destination = int.TryParse(configuration["FBClientsImport:Rehearsal:InventoryModuleId"], out var id) && id > 0 ? id : null;
            return new(ReadInventoryPlan(moduleId, false), enabled && destination.HasValue, destination);
        }

        private InventoryImportPlan ReadInventoryPlan(int moduleId, bool required = true)
        {
            var stored = settings.GetSetting(EntityNames.Module, moduleId, InventoryImportSelection.PlanSetting)?.SettingValue;
            if (string.IsNullOrEmpty(stored))
            {
                if (required) throw new ValidationException("Save a separate inventory selection before running inventory reports or rehearsal.");
                return null;
            }
            try { return InventoryImportSelection.Normalize(JsonSerializer.Deserialize<InventoryImportPlan>(stored)); }
            catch (JsonException ex)
            {
                logger.Log(LogLevel.Error, this, LogFunction.Read, "Inventory import plan is invalid for module {ModuleId}: {ErrorType}", moduleId, ex.GetType().Name);
                throw new ImportDataException("The saved inventory selection is invalid. Save it again or review the server logs.");
            }
        }

        public async Task<List<ImportSourceTable>> InspectInventoryAsync(int moduleId, string source)
        {
            var (tenant, site) = await AuthorizeAsync(moduleId);
            return await InventoryDatabaseOperationAsync(moduleId, async () =>
            {
                await using var connection = new SqlConnection(InventorySourceConnection(tenant, site, source, moduleId));
                await connection.OpenAsync();
                var results = new List<ImportSourceTable>();
                foreach (var table in InventoryImportSelection.Tables)
                    results.Add(new(table.Name, table.Description, (await ColumnsAsync(connection, table.Name)).Count > 0));
                return results;
            });
        }

        public async Task SaveInventoryPlanAsync(int moduleId, InventoryImportPlan plan)
        {
            var (_, site) = await AuthorizeAsync(moduleId, edit: true);
            plan = InventoryImportSelection.Normalize(plan);
            ValidateInventoryDestinations(plan, site, false);
            var available = await InspectInventoryAsync(moduleId, plan.Source);
            if (plan.Tables.Any(name => !available.Any(table => table.Name == name && table.Available)))
                throw new ValidationException("A selected table or automatically included dependency is unavailable. Grant source access or change the selection.");
            var setting = settings.GetSetting(EntityNames.Module, moduleId, InventoryImportSelection.PlanSetting);
            var value = JsonSerializer.Serialize(plan);
            if (setting == null)
                settings.AddSetting(new Setting { EntityName = EntityNames.Module, EntityId = moduleId,
                    SettingName = InventoryImportSelection.PlanSetting, SettingValue = value, IsPrivate = true });
            else
            {
                setting.SettingValue = value;
                setting.IsPrivate = true;
                settings.UpdateSetting(setting);
            }
        }

        private void ValidateInventoryDestinations(InventoryImportPlan plan, int site, bool rehearsal)
        {
            var inventory = modules.GetModule(plan.DestinationInventoryModuleId);
            var clients = modules.GetModule(plan.DestinationClientsModuleId);
            if (inventory == null || inventory.IsDeleted || inventory.SiteId != site || inventory.ModuleDefinitionName != InventoryModule.DefinitionName
                || clients == null || clients.IsDeleted || clients.SiteId != site || clients.ModuleDefinitionName != FulfillmentModule.ClientsDefinitionName)
                throw new ValidationException("Choose an Inventory module and an FBClients module on this site.");
            if (!rehearsal) return;
            if (!bool.TryParse(configuration["FBClientsImport:Rehearsal:Enabled"], out var enabled) || !enabled
                || !int.TryParse(configuration["FBClientsImport:Rehearsal:InventoryModuleId"], out var inventoryId) || inventoryId != inventory.ModuleId
                || !int.TryParse(configuration["FBClientsImport:Rehearsal:DestinationModuleId"], out var clientsId) || clientsId != clients.ModuleId)
                throw new ValidationException("The host must enable rehearsal and explicitly configure both isolated InventoryModuleId and DestinationModuleId. No production import is available.");
        }

        private string InventorySourceConnection(int tenant, int site, string source, int moduleId)
        {
            var builder = new SqlConnectionStringBuilder(ResolveSourceConnection(tenant, site, source, moduleId))
                { ApplicationName = "FBClients Inventory Import", ApplicationIntent = ApplicationIntent.ReadOnly };
            if (string.IsNullOrWhiteSpace(builder.InitialCatalog)) throw new ValidationException("Specify a source database.");
            return builder.ConnectionString;
        }

        private async Task<T> InventoryDatabaseOperationAsync<T>(int moduleId, Func<Task<T>> action)
        {
            try { return await action(); }
            catch (SqlException ex)
            {
                logger.Log(LogLevel.Error, this, LogFunction.Other,
                    "Inventory import failed for module {ModuleId}: SQL {Number}, state {State}, procedure {Procedure}, line {Line}.",
                    moduleId, ex.Number, ex.State, ex.Procedure ?? "", ex.LineNumber);
                throw new ImportDataException("Inventory import could not complete. Check source permissions, destination schema, connectivity, and the SQL error in server logs. Committed checkpoints remain durable; do not delete them.");
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
            {
                logger.Log(LogLevel.Error, this, LogFunction.Other, "Inventory import failed for module {ModuleId}: {ErrorType}", moduleId, ex.GetType().Name);
                throw new ImportDataException("Inventory import could not complete. Review the saved plan, database schema, and server logs.");
            }
        }
    }
}
