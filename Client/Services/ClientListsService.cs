using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Oqtane.Services;
using Oqtane.Shared;

namespace GIBS.Module.FBClients.Lists.Services
{

    public class ClientListsService : ServiceBase, IListsService
    {
        public ClientListsService(HttpClient http, SiteState siteState) : base(http, siteState) { }

        private string Apiurl => CreateApiUrl("FBClientsLists");

        public async Task<List<Models.Lists>> GetListssAsync(int ModuleId)
        {
            var lists = await GetHttpClient().GetFromJsonAsync<List<Models.Lists>>(CreateAuthorizationPolicyUrl($"{Apiurl}?moduleid={ModuleId}", EntityNames.Module, ModuleId))
                ?? throw new InvalidOperationException("The lists response was empty.");
            return lists;
        }

        public async Task<List<Models.Lists>> GetListsAsync(string ListName, int SiteId)
        {
            var url = $"{Apiurl}/by-name?siteid={SiteId}&listName={Uri.EscapeDataString(ListName ?? string.Empty)}";
            return await GetHttpClient().GetFromJsonAsync<List<Models.Lists>>(url)
                ?? throw new InvalidOperationException("The lists response was empty.");
        }

        public async Task<List<Models.Lists>> GetChildListsAsync(int parentId, int siteId)
        {
            return await GetHttpClient().GetFromJsonAsync<List<Models.Lists>>($"{Apiurl}/children/{parentId}?siteid={siteId}")
                ?? throw new InvalidOperationException("The lists response was empty.");
        }

        public async Task<Models.Lists> GetListsAsync(int ListsId, int ModuleId)
        {
            return await GetJsonAsync<Models.Lists>(CreateAuthorizationPolicyUrl($"{Apiurl}/{ListsId}/{ModuleId}", EntityNames.Module, ModuleId));
        }

        public async Task<Models.Lists> AddListsAsync(Models.Lists Lists, bool createNewList = false)
        {
            return await PostJsonAsync<Models.Lists>(CreateAuthorizationPolicyUrl($"{Apiurl}?createNewList={createNewList}", EntityNames.Module, Lists.ModuleId), Lists);
        }

        public async Task<Models.Lists> UpdateListsAsync(Models.Lists Lists)
        {
            return await PutJsonAsync<Models.Lists>(CreateAuthorizationPolicyUrl($"{Apiurl}/{Lists.ListsId}", EntityNames.Module, Lists.ModuleId), Lists);
        }

        public async Task DeleteListsAsync(int ListsId, int ModuleId)
        {
            using var response = await GetHttpClient().DeleteAsync(CreateAuthorizationPolicyUrl($"{Apiurl}/{ListsId}/{ModuleId}", EntityNames.Module, ModuleId));
            response.EnsureSuccessStatusCode();
        }

        public async Task DeleteListAsync(int ListsId, int ModuleId)
        {
            using var response = await GetHttpClient().DeleteAsync(CreateAuthorizationPolicyUrl($"{Apiurl}/list/{ListsId}/{ModuleId}", EntityNames.Module, ModuleId));
            response.EnsureSuccessStatusCode();
        }

        public async Task MoveListsAsync(int ListsId, int direction, int ModuleId)
        {
            using var response = await GetHttpClient().PutAsync(CreateAuthorizationPolicyUrl($"{Apiurl}/move/{ListsId}/{direction}/{ModuleId}", EntityNames.Module, ModuleId), null);
            response.EnsureSuccessStatusCode();
        }
    }
}
