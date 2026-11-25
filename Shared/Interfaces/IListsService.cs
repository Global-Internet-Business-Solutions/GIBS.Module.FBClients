using System.Collections.Generic;
using System.Threading.Tasks;

namespace GIBS.Module.FBClients.Lists.Services
{
    public interface IListsService 
    {
        Task<List<Models.Lists>> GetListssAsync(int ModuleId);

        Task<List<Models.Lists>> GetListsAsync(string ListName, int SiteId);

        Task<List<Models.Lists>> GetChildListsAsync(int parentId, int siteId);

        Task<Models.Lists> GetListsAsync(int ListsId, int ModuleId);

        Task<Models.Lists> AddListsAsync(Models.Lists Lists, bool createNewList = false);

        Task<Models.Lists> UpdateListsAsync(Models.Lists Lists);

        Task DeleteListsAsync(int ListsId, int ModuleId);

        Task DeleteListAsync(int ListsId, int ModuleId);

        Task MoveListsAsync(int ListsId, int direction, int ModuleId);
    }
}
