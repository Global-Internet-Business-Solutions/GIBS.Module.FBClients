using System.Collections.Generic;
using System.Threading.Tasks;

namespace GIBS.Module.FBClients.Services
{
    public interface IFBClientsService 
    {
        Task<List<Models.FBClients>> GetFBClientssAsync(int ModuleId);

        Task<Models.FBClients> GetFBClientsAsync(int FBClientsId, int ModuleId);

        Task<Models.FBClients> AddFBClientsAsync(Models.FBClients FBClients);

        Task<Models.FBClients> UpdateFBClientsAsync(Models.FBClients FBClients);

        Task DeleteFBClientsAsync(int FBClientsId, int ModuleId);
    }
}
