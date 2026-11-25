using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Oqtane.Services;
using Oqtane.Shared;

namespace GIBS.Module.FBClients.Inventory.Services
{
    public class ClientInventoryService(HttpClient http, SiteState state) : ServiceBase(http, state), IInventoryService
    {
        public Task<InventoryCatalog> GetCatalogAsync(int moduleId) => GetAsync<InventoryCatalog>(moduleId, "catalog");
        public Task<InventoryPage<InventoryInvoice>> GetInvoicesAsync(int moduleId, int page, string search) =>
            GetAsync<InventoryPage<InventoryInvoice>>(moduleId, $"invoices?page={page}&search={Uri.EscapeDataString(search ?? "")}");
        public Task<InventoryInvoiceDetails> GetInvoiceAsync(int moduleId, int invoiceId) =>
            GetAsync<InventoryInvoiceDetails>(moduleId, $"invoices/{invoiceId}");
        public Task<List<InventoryReportRow>> GetReportAsync(int moduleId, DateOnly start, DateOnly end) =>
            GetAsync<List<InventoryReportRow>>(moduleId, FormattableString.Invariant($"report?start={start:yyyy-MM-dd}&end={end:yyyy-MM-dd}"));
        public Task<int> SaveCategoryAsync(int moduleId, InventoryCategory input) => SaveAsync(moduleId, "categories", input);
        public Task<int> SaveProductAsync(int moduleId, InventoryProduct input) => SaveAsync(moduleId, "products", input);
        public Task<int> SaveSupplierAsync(int moduleId, InventorySupplier input) => SaveAsync(moduleId, "suppliers", input);
        public Task<int> SaveInvoiceAsync(int moduleId, InventoryInvoice input) => SaveAsync(moduleId, "invoices", input);
        public Task<int> SaveLineAsync(int moduleId, int invoiceId, InventoryLine input) => SaveAsync(moduleId, $"invoices/{invoiceId}/lines", input);
        public Task DeleteInvoiceAsync(int moduleId, int invoiceId) => DeleteAsync(moduleId, $"invoices/{invoiceId}");
        public Task DeleteLineAsync(int moduleId, int invoiceId, int lineId) => DeleteAsync(moduleId, $"invoices/{invoiceId}/lines/{lineId}");

        private string Url(int moduleId, string path) =>
            CreateAuthorizationPolicyUrl($"{CreateApiUrl("FBClientsInventory")}/{moduleId}/{path}", EntityNames.Module, moduleId);

        private async Task<T> GetAsync<T>(int moduleId, string path)
        {
            using var response = await GetHttpClient().GetAsync(Url(moduleId, path));
            await CheckAsync(response);
            return await response.Content.ReadFromJsonAsync<T>() ?? throw new HttpRequestException("The inventory response was empty.");
        }

        private async Task<int> SaveAsync<T>(int moduleId, string path, T input)
        {
            using var response = await GetHttpClient().PostAsJsonAsync(Url(moduleId, path), input);
            await CheckAsync(response);
            return await response.Content.ReadFromJsonAsync<int>();
        }

        private async Task DeleteAsync(int moduleId, string path)
        {
            using var response = await GetHttpClient().DeleteAsync(Url(moduleId, path));
            await CheckAsync(response);
        }

        private static async Task CheckAsync(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode) return;
            if (response.Content.Headers.ContentType?.MediaType == "application/problem+json")
            {
                var problem = await response.Content.ReadFromJsonAsync<InventoryProblem>();
                var detail = problem?.Detail ?? problem?.Errors?.Values.SelectMany(messages => messages).FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(detail)) throw new ValidationException(detail);
            }
            response.EnsureSuccessStatusCode();
        }

        private record InventoryProblem(string Detail, Dictionary<string, string[]> Errors);
    }
}
