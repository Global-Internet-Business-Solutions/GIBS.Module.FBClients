using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Oqtane.Services;
using Oqtane.Shared;

namespace GIBS.Module.FBClients.Services
{

    public class ClientFBClientsService : ServiceBase, IFBClientsService
    {
        public ClientFBClientsService(HttpClient http, SiteState siteState) : base(http, siteState) { }

        private string Apiurl => CreateApiUrl("FBClients");

        public async Task<List<Models.FBClients>> GetFBClientssAsync(int ModuleId)
        {
            List<Models.FBClients> FBClientss = await GetJsonAsync<List<Models.FBClients>>(CreateAuthorizationPolicyUrl($"{Apiurl}?moduleid={ModuleId}", EntityNames.Module, ModuleId), Enumerable.Empty<Models.FBClients>().ToList());
            return FBClientss.OrderBy(item => item.Name).ToList();
        }

        public async Task<Models.FBClients> GetFBClientsAsync(int FBClientsId, int ModuleId)
        {
            return await GetJsonAsync<Models.FBClients>(CreateAuthorizationPolicyUrl($"{Apiurl}/{FBClientsId}/{ModuleId}", EntityNames.Module, ModuleId));
        }

        public async Task<Models.FBClients> AddFBClientsAsync(Models.FBClients FBClients)
        {
            return await PostJsonAsync<Models.FBClients>(CreateAuthorizationPolicyUrl($"{Apiurl}", EntityNames.Module, FBClients.ModuleId), FBClients);
        }

        public async Task<Models.FBClients> UpdateFBClientsAsync(Models.FBClients FBClients)
        {
            return await PutJsonAsync<Models.FBClients>(CreateAuthorizationPolicyUrl($"{Apiurl}/{FBClients.FBClientsId}", EntityNames.Module, FBClients.ModuleId), FBClients);
        }

        public async Task DeleteFBClientsAsync(int FBClientsId, int ModuleId)
        {
            await DeleteAsync(CreateAuthorizationPolicyUrl($"{Apiurl}/{FBClientsId}/{ModuleId}", EntityNames.Module, ModuleId));
        }
    }
}
