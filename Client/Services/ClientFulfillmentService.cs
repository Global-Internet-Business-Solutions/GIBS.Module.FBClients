using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using GIBS.Module.FBClients.Models;
using Oqtane.Services;
using Oqtane.Shared;

namespace GIBS.Module.FBClients.Fulfillment
{
    public class ClientFulfillmentService(HttpClient http, SiteState state) : ServiceBase(http, state), IFulfillmentService
    {
        public async Task<FulfillmentQueue> GetQueueAsync(int moduleId, DateOnly date, int? status, string location, int page)
        {
            var query = FormattableString.Invariant($"queue?date={date:yyyy-MM-dd}&page={page}&location={Uri.EscapeDataString(location ?? string.Empty)}");
            if (status.HasValue) query += FormattableString.Invariant($"&status={status.Value}");
            using var response = await GetHttpClient().GetAsync(Url(moduleId, query));
            return await ReadAsync<FulfillmentQueue>(response);
        }

        public async Task<FulfillmentOrder> GetOrderAsync(int moduleId, int visitId)
        {
            using var response = await GetHttpClient().GetAsync(Url(moduleId, OrderPath(visitId)));
            return await ReadAsync<FulfillmentOrder>(response);
        }

        public async Task SubmitPaperOrderAsync(int moduleId, int visitId, Dictionary<int, int> quantities)
        {
            using var response = await GetHttpClient().PostAsJsonAsync(Url(moduleId, $"{OrderPath(visitId)}/paper"), quantities);
            await CheckAsync(response);
        }

        public async Task MarkFilledAsync(int moduleId, int visitId, int[] checkedItems)
        {
            using var response = await GetHttpClient().PostAsJsonAsync(Url(moduleId, $"{OrderPath(visitId)}/filled"), checkedItems);
            await CheckAsync(response);
        }

        public async Task<FBClientsOrderTextResult> SendPickupTextAsync(int moduleId, int visitId)
        {
            using var response = await GetHttpClient().PostAsync(Url(moduleId, $"{OrderPath(visitId)}/pickup-text"), null);
            return await ReadAsync<FBClientsOrderTextResult>(response);
        }

        private static string OrderPath(int visitId) => FormattableString.Invariant($"orders/{visitId}");
        private string Url(int moduleId, string path) => CreateAuthorizationPolicyUrl(
            FormattableString.Invariant($"{CreateApiUrl("FBClientsFulfillment")}/{moduleId}/{path}"), EntityNames.Module, moduleId);

        private static async Task<T> ReadAsync<T>(HttpResponseMessage response)
        {
            await CheckAsync(response);
            return await response.Content.ReadFromJsonAsync<T>() ?? throw new HttpRequestException("The fulfillment response was empty.");
        }

        private static async Task CheckAsync(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode) return;
            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden) throw new UnauthorizedAccessException();
            if (response.Content.Headers.ContentType?.MediaType == "application/problem+json"
                && await response.Content.ReadFromJsonAsync<ProblemResponse>() is { Detail: { Length: > 0 } detail })
            {
                if (response.StatusCode == HttpStatusCode.BadRequest) throw new ValidationException(detail);
                if (response.StatusCode == HttpStatusCode.NotFound) throw new KeyNotFoundException(detail);
                if (response.StatusCode == HttpStatusCode.Conflict) throw new FulfillmentConflictException(detail);
            }
            response.EnsureSuccessStatusCode();
        }

        private record ProblemResponse(string Detail);
    }
}
