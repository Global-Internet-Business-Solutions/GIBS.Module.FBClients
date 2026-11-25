using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using GIBS.Module.FBClients.Repository;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Oqtane.Enums;
using Oqtane.Infrastructure;
using Oqtane.Models;
using Oqtane.Repository;
using Oqtane.Security;
using Oqtane.Shared;

namespace GIBS.Module.FBClients.ImportData
{
    public partial class ServerImportDataService(IConfigurationRoot configuration, ITenantManager tenants, IModuleRepository modules,
        ISettingRepository settings, IUserPermissions permissions, IHttpContextAccessor http,
        AuthenticationStateProvider authentication, ILogManager logger, IDataProtectionProvider protection,
        IDbContextFactory<FBClientsContext> factory) : IImportDataService
    {
        private static readonly (string Name, string Description)[] Tables =
        [
            ("GIBS_FBClients", "Clients"),
            ("GIBS_FBClientsAddFamMem", "Family members"),
            ("GIBS_FBClientsVisits", "Visits"),
            ("GIBS_FBClientsIncomeAndExpense", "Income and expenses"),
            ("GIBS_FBClientsTrueFalseQuestions", "Client questions"),
            ("GIBS_FBClientsIDPhoto", "Client ID photos"),
            ("GIBS_FBxMas", "Christmas records"),
            ("GIBS_FBClients_Merged", "Merged client history"),
            ("GIBS_FBClientsAddFamMem_Merged", "Merged family history"),
            ("GIBS_FBClientsVisits_Merged", "Merged visit history"),
            ("GIBS_FBClientsIncomeAndExpense_Merged", "Merged income and expense history"),
            ("GIBS_FBClientsTrueFalseQuestions_Merged", "Merged client questions history"),
            ("GIBS_FBxMas_Merged", "Merged Christmas history")
        ];

        private async Task<(int TenantId, int SiteId)> AuthorizeAsync(int moduleId, bool edit = false)
        {
            var alias = tenants.GetAlias();
            ClaimsPrincipal user = http.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true && authentication != null)
                user = (await authentication.GetAuthenticationStateAsync()).User;
            var module = alias == null ? null : modules.GetModule(moduleId);
            if (alias == null || user?.Identity?.IsAuthenticated != true || module == null || module.IsDeleted
                || module.SiteId != alias.SiteId || module.ModuleDefinitionName != ImportDataModule.DefinitionName
                || !UserSecurity.IsAuthorized(permissions.GetUser(user), RoleNames.Admin)
                || !permissions.IsAuthorized(user, alias.SiteId, EntityNames.Module, moduleId, edit ? PermissionNames.Edit : PermissionNames.View))
                throw new UnauthorizedAccessException();
            return (alias.TenantId, alias.SiteId);
        }

        private List<IConfigurationSection> Sources(int tenantId, int siteId) => configuration.GetSection("FBClientsImport:Sources").GetChildren()
            .Where(s => !string.Equals(s.Key, ImportDataModule.ModuleSource, StringComparison.OrdinalIgnoreCase)
                && int.TryParse(s["TenantId"], out var tenant) && tenant == tenantId
                && int.TryParse(s["SiteId"], out var site) && site == siteId).ToList();

        private IDataProtector Protector(int tenant, int site, int moduleId) => protection.CreateProtector(
            "GIBS.Module.FBClients.ImportData.Connection", tenant.ToString(System.Globalization.CultureInfo.InvariantCulture),
            site.ToString(System.Globalization.CultureInfo.InvariantCulture), moduleId.ToString(System.Globalization.CultureInfo.InvariantCulture));

        public async Task<bool> HasConnectionAsync(int moduleId)
        {
            await AuthorizeAsync(moduleId, edit: true);
            return !string.IsNullOrEmpty(settings.GetSetting(EntityNames.Module, moduleId, ImportDataModule.ConnectionSetting)?.SettingValue);
        }

        public async Task SaveConnectionAsync(int moduleId, ImportConnectionChange change)
        {
            var (tenant, site) = await AuthorizeAsync(moduleId, edit: true);
            if (change == null || (change.Remove && !string.IsNullOrWhiteSpace(change.ConnectionString)))
                throw new ValidationException("Choose either a replacement connection string or removal, not both.");
            var existing = settings.GetSetting(EntityNames.Module, moduleId, ImportDataModule.ConnectionSetting);
            if (change.Remove)
            {
                if (existing != null) settings.DeleteSetting(EntityNames.Module, existing.SettingId);
                return;
            }
            if (string.IsNullOrWhiteSpace(change.ConnectionString))
            {
                if (existing == null) throw new ValidationException("Enter a connection string or use a server-configured source.");
                return;
            }
            if (change.ConnectionString.Length > 4096) throw new ValidationException("The connection string is too long.");
            try
            {
                var builder = new SqlConnectionStringBuilder(change.ConnectionString);
                if (string.IsNullOrWhiteSpace(builder.DataSource) || string.IsNullOrWhiteSpace(builder.InitialCatalog)
                    || !string.IsNullOrEmpty(builder.AttachDBFilename))
                    throw new ValidationException("Specify a SQL Server and DNN database; attached database files are not supported.");
            }
            catch (ArgumentException) { throw new ValidationException("Enter a valid SQL Server connection string."); }
            var encrypted = Protector(tenant, site, moduleId).Protect(change.ConnectionString);
            if (existing == null)
                settings.AddSetting(new Setting { EntityName = EntityNames.Module, EntityId = moduleId, SettingName = ImportDataModule.ConnectionSetting, SettingValue = encrypted, IsPrivate = true });
            else
            {
                existing.SettingValue = encrypted;
                existing.IsPrivate = true;
                settings.UpdateSetting(existing);
            }
        }

        public async Task<ImportDataConfiguration> GetConfigurationAsync(int moduleId)
        {
            var (tenant, site) = await AuthorizeAsync(moduleId);
            var sources = Sources(tenant, site).Select(s => new ImportSource(s.Key, s["Name"] ?? s.Key)).ToList();
            if (!string.IsNullOrEmpty(settings.GetSetting(EntityNames.Module, moduleId, ImportDataModule.ConnectionSetting)?.SettingValue))
                sources.Insert(0, new ImportSource(ImportDataModule.ModuleSource, "Module connection"));
            var stored = settings.GetSetting(EntityNames.Module, moduleId, ImportDataModule.PlanSetting)?.SettingValue;
            ImportDataPlan plan = null;
            if (!string.IsNullOrEmpty(stored))
            {
                try { plan = JsonSerializer.Deserialize<ImportDataPlan>(stored); }
                catch (JsonException ex)
                {
                    logger.Log(LogLevel.Warning, this, LogFunction.Read, "Import plan is invalid for module {ModuleId}: {ErrorType}", moduleId, ex.GetType().Name);
                    throw new ImportDataException("The saved import selection is invalid. Contact the site administrator.");
                }
                if (plan?.Source == null || plan.Tables == null) throw new ImportDataException("The saved import selection is invalid. Contact the site administrator.");
            }
            var rehearsalEnabled = bool.TryParse(configuration["FBClientsImport:Rehearsal:Enabled"], out var enabled) && enabled;
            int? rehearsalDestination = int.TryParse(configuration["FBClientsImport:Rehearsal:DestinationModuleId"], out var destinationId) && destinationId > 0
                ? destinationId : null;
            return new(sources, plan, rehearsalEnabled, rehearsalDestination);
        }

        public async Task<List<ImportSourceTable>> InspectAsync(int moduleId, string source)
        {
            var (tenant, site) = await AuthorizeAsync(moduleId);
            return await InspectSourceAsync(tenant, site, source, moduleId);
        }

        private string ResolveSourceConnection(int tenant, int site, string source, int moduleId)
        {
            string connectionString;
            if (string.Equals(source, ImportDataModule.ModuleSource, StringComparison.Ordinal))
            {
                var encrypted = settings.GetSetting(EntityNames.Module, moduleId, ImportDataModule.ConnectionSetting)?.SettingValue;
                if (string.IsNullOrEmpty(encrypted)) throw new ValidationException("Configure the DNN connection in Import Data settings.");
                try { connectionString = Protector(tenant, site, moduleId).Unprotect(encrypted); }
                catch (CryptographicException ex)
                {
                    logger.Log(LogLevel.Error, this, LogFunction.Read, "Import connection could not be decrypted for module {ModuleId}: {ErrorType}", moduleId, ex.GetType().Name);
                    throw new ImportDataException("The saved connection cannot be decrypted. Check the server's Data Protection keys or replace the connection in settings.");
                }
            }
            else
            {
                var entry = Sources(tenant, site).SingleOrDefault(x => string.Equals(x.Key, source, StringComparison.Ordinal));
                if (entry == null) throw new ValidationException("Select a configured DNN source for this site.");
                var connectionName = entry["ConnectionStringName"];
                connectionString = string.IsNullOrWhiteSpace(connectionName) ? null : configuration.GetConnectionString(connectionName);
                if (string.IsNullOrWhiteSpace(connectionString)) throw new ValidationException("The selected source has no server-side connection string configured.");
            }
            return connectionString;
        }

        private async Task<List<ImportSourceTable>> InspectSourceAsync(int tenant, int site, string source, int moduleId)
        {
            var connectionString = ResolveSourceConnection(tenant, site, source, moduleId);
            try
            {
                var builder = new SqlConnectionStringBuilder(connectionString) { ApplicationName = "FBClients Import Data", ApplicationIntent = ApplicationIntent.ReadOnly };
                if (string.IsNullOrWhiteSpace(builder.InitialCatalog))
                    throw new ValidationException("The selected source connection string must specify the DNN database.");
                await using var connection = new SqlConnection(builder.ConnectionString);
                await connection.OpenAsync();
                await using var command = connection.CreateCommand();
                command.CommandTimeout = 30;
                command.CommandText = "SELECT t.name FROM sys.tables AS t INNER JOIN sys.schemas AS s ON s.schema_id = t.schema_id WHERE s.name = @schema AND t.name IN ("
                    + string.Join(",", Tables.Select((_, index) => "@t" + index)) + ")";
                command.Parameters.Add(new SqlParameter("@schema", System.Data.SqlDbType.NVarChar, 128) { Value = "dbo" });
                for (var i = 0; i < Tables.Length; i++)
                    command.Parameters.Add(new SqlParameter("@t" + i, System.Data.SqlDbType.NVarChar, 128) { Value = Tables[i].Name });
                var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync()) found.Add(reader.GetString(0));
                return Tables.Select(table => new ImportSourceTable(table.Name, table.Description, found.Contains(table.Name))).ToList();
            }
            catch (Exception ex) when (ex is SqlException or ArgumentException or InvalidOperationException)
            {
                logger.Log(LogLevel.Error, this, LogFunction.Read, "DNN source inspection failed for module {ModuleId}: {ErrorType}", moduleId, ex.GetType().Name);
                throw new ImportDataException("Could not inspect the DNN database. Check the server connection configuration, SQL connectivity, and metadata permissions.");
            }
        }

        public async Task SavePlanAsync(int moduleId, ImportDataPlan plan)
        {
            var (tenant, site) = await AuthorizeAsync(moduleId, edit: true);
            if (plan == null || string.IsNullOrWhiteSpace(plan.Source) || plan.Tables == null || plan.Tables.Count == 0
                || plan.Tables.Count > Tables.Length || plan.Tables.Any(string.IsNullOrWhiteSpace)
                || plan.Tables.Distinct(StringComparer.Ordinal).Count() != plan.Tables.Count)
                throw new ValidationException("Select a source and at least one available table without duplicates.");
            if (plan.SourceModuleId is null or <= 0)
                throw new ValidationException("Enter the DNN source ModuleID before saving the selection.");
            var available = await InspectSourceAsync(tenant, site, plan.Source, moduleId);
            var allowed = available.Where(x => x.Available).Select(x => x.Name).ToHashSet(StringComparer.Ordinal);
            if (plan.Tables.Any(name => !allowed.Contains(name)))
                throw new ValidationException("The selection contains a table that is unavailable or not supported. Inspect the source again.");
            var value = JsonSerializer.Serialize(plan);
            var setting = settings.GetSetting(EntityNames.Module, moduleId, ImportDataModule.PlanSetting);
            if (setting == null)
                settings.AddSetting(new Setting { EntityName = EntityNames.Module, EntityId = moduleId, SettingName = ImportDataModule.PlanSetting, SettingValue = value, IsPrivate = true });
            else
            {
                setting.SettingValue = value;
                setting.IsPrivate = true;
                settings.UpdateSetting(setting);
            }
        }
    }
}
