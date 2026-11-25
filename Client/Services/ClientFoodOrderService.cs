using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Oqtane.Services;
using Oqtane.Shared;

namespace GIBS.Module.FBClients.Ordering.Services
{
    public class ClientFoodOrderService(HttpClient http, SiteState siteState) : ServiceBase(http, siteState), IFoodOrderService
    {
        public async Task<FoodOrderSheet> OpenAsync(int moduleId, string key)
        {
            using var response = await GetHttpClient().PostAsJsonAsync($"{CreateApiUrl("FBClientsOrdering")}/{moduleId}/open",
                new FoodOrderRequest { Key = key });
            if (response.StatusCode == HttpStatusCode.NotFound) return null;
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<FoodOrderSheet>();
        }

        public async Task<FoodOrderSubmitResult> SubmitAsync(int moduleId, string key, Dictionary<int, int> quantities)
        {
            using var response = await GetHttpClient().PostAsJsonAsync($"{CreateApiUrl("FBClientsOrdering")}/{moduleId}/submit",
                new FoodOrderRequest { Key = key, Quantities = quantities });
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<FoodOrderSubmitResult>();
        }
    }
}
