using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Oqtane.Services;
using Oqtane.Shared;

namespace GIBS.Module.FBClients.ImportData
{
    public class ClientImportDataService(HttpClient http, SiteState state) : ServiceBase(http, state), IImportDataService
    {
        public async Task<bool> HasConnectionAsync(int moduleId)
        {
            using var response = await GetHttpClient().GetAsync(Url(moduleId, "connection"));
            return await ReadAsync<bool>(response);
        }

        public async Task SaveConnectionAsync(int moduleId, ImportConnectionChange change)
        {
            using var response = await GetHttpClient().PostAsJsonAsync(Url(moduleId, "connection"), change);
            await CheckAsync(response);
        }

        public async Task<ImportDataConfiguration> GetConfigurationAsync(int moduleId)
        {
            using var response = await GetHttpClient().GetAsync(Url(moduleId, ""));
            return await ReadAsync<ImportDataConfiguration>(response);
        }

        public async Task<List<ImportSourceTable>> InspectAsync(int moduleId, string source)
        {
            using var response = await GetHttpClient().GetAsync(Url(moduleId, "tables?source=" + Uri.EscapeDataString(source)));
            return await ReadAsync<List<ImportSourceTable>>(response);
        }

        public async Task SavePlanAsync(int moduleId, ImportDataPlan plan)
        {
            using var response = await GetHttpClient().PostAsJsonAsync(Url(moduleId, "plan"), plan);
            await CheckAsync(response);
        }

        public async Task<ImportPreflightReport> PreflightAsync(int moduleId, ImportPreflightRequest request)
        {
            using var response = await GetHttpClient().PostAsJsonAsync(Url(moduleId, "preflight"), request);
            return await ReadAsync<ImportPreflightReport>(response);
        }

        public async Task<ImportReconciliationReport> ReconcileAsync(int moduleId, ImportPreflightRequest request)
        {
            using var response = await GetHttpClient().PostAsJsonAsync(Url(moduleId, "reconciliation"), request);
            return await ReadAsync<ImportReconciliationReport>(response);
        }

        public async Task<ImportBatchRunReport> RunRehearsalBatchAsync(int moduleId, ImportRehearsalRequest request)
        {
            using var response = await GetHttpClient().PostAsJsonAsync(Url(moduleId, "rehearsal"), request);
            return await ReadAsync<ImportBatchRunReport>(response);
        }

        public async Task<ImportBatchRunReport> RunTurkeyRehearsalBatchAsync(int moduleId, ImportRehearsalRequest request)
        {
            using var response = await GetHttpClient().PostAsJsonAsync(Url(moduleId, "turkey-rehearsal"), request);
            return await ReadAsync<ImportBatchRunReport>(response);
        }

        public async Task<InventoryImportConfiguration> GetInventoryConfigurationAsync(int moduleId)
        {
            using var response = await GetHttpClient().GetAsync(Url(moduleId, "inventory"));
            return await ReadAsync<InventoryImportConfiguration>(response);
        }

        public async Task<List<ImportSourceTable>> InspectInventoryAsync(int moduleId, string source)
        {
            using var response = await GetHttpClient().GetAsync(Url(moduleId, "inventory/tables?source=" + Uri.EscapeDataString(source)));
            return await ReadAsync<List<ImportSourceTable>>(response);
        }

        public async Task SaveInventoryPlanAsync(int moduleId, InventoryImportPlan plan)
        {
            using var response = await GetHttpClient().PostAsJsonAsync(Url(moduleId, "inventory/plan"), plan);
            await CheckAsync(response);
        }

        public async Task<InventoryImportReport> InventoryPreflightAsync(int moduleId)
        {
            using var response = await GetHttpClient().PostAsJsonAsync(Url(moduleId, "inventory/preflight"), new { });
            return await ReadAsync<InventoryImportReport>(response);
        }

        public async Task<InventoryImportReport> InventoryReconcileAsync(int moduleId)
        {
            using var response = await GetHttpClient().PostAsJsonAsync(Url(moduleId, "inventory/reconciliation"), new { });
            return await ReadAsync<InventoryImportReport>(response);
        }

        public async Task<ImportBatchRunReport> RunInventoryRehearsalAsync(int moduleId, InventoryImportRunRequest request)
        {
            using var response = await GetHttpClient().PostAsJsonAsync(Url(moduleId, "inventory/rehearsal"), request);
            return await ReadAsync<ImportBatchRunReport>(response);
        }

        private string Url(int moduleId, string path) => CreateAuthorizationPolicyUrl(
            FormattableString.Invariant($"{CreateApiUrl("FBClientsImportData")}/{moduleId}/{path}"), EntityNames.Module, moduleId);

        private static async Task<T> ReadAsync<T>(HttpResponseMessage response)
        {
            await CheckAsync(response);
            return await response.Content.ReadFromJsonAsync<T>() ?? throw new HttpRequestException("The import response was empty.");
        }

        private static async Task CheckAsync(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode) return;
            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden) throw new UnauthorizedAccessException();
            if (response.Content.Headers.ContentType?.MediaType == "application/problem+json"
                && await response.Content.ReadFromJsonAsync<ProblemResponse>() is { Detail: { Length: > 0 } detail })
            {
                if (response.StatusCode == HttpStatusCode.BadRequest) throw new ValidationException(detail);
                if (response.StatusCode == HttpStatusCode.ServiceUnavailable) throw new ImportDataException(detail);
            }
            response.EnsureSuccessStatusCode();
        }

        private record ProblemResponse(string Detail);
    }
}
