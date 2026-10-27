using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Oqtane.Modules;
using Oqtane.Models;
using Oqtane.Infrastructure;
using Oqtane.Interfaces;
using Oqtane.Enums;
using Oqtane.Repository;
using GIBS.Module.FBClients.Repository;
using System.Threading.Tasks;

namespace GIBS.Module.FBClients.Manager
{
    public class FBClientsManager : MigratableModuleBase, IInstallable, IPortable, ISearchable
    {
        private readonly IFBClientsRepository _FBClientsRepository;
        private readonly IDBContextDependencies _DBContextDependencies;

        public FBClientsManager(IFBClientsRepository FBClientsRepository, IDBContextDependencies DBContextDependencies)
        {
            _FBClientsRepository = FBClientsRepository;
            _DBContextDependencies = DBContextDependencies;
        }

        public bool Install(Tenant tenant, string version)
        {
            return Migrate(new FBClientsContext(_DBContextDependencies), tenant, MigrationType.Up);
        }

        public bool Uninstall(Tenant tenant)
        {
            return Migrate(new FBClientsContext(_DBContextDependencies), tenant, MigrationType.Down);
        }

        public string ExportModule(Oqtane.Models.Module module)
        {
            string content = "";
            List<Models.FBClients> FBClientss = _FBClientsRepository.GetFBClientss(module.ModuleId).ToList();
            if (FBClientss != null)
            {
                content = JsonSerializer.Serialize(FBClientss);
            }
            return content;
        }

        public void ImportModule(Oqtane.Models.Module module, string content, string version)
        {
            List<Models.FBClients> FBClientss = null;
            if (!string.IsNullOrEmpty(content))
            {
                FBClientss = JsonSerializer.Deserialize<List<Models.FBClients>>(content);
            }
            if (FBClientss != null)
            {
                foreach(var FBClients in FBClientss)
                {
                    _FBClientsRepository.AddFBClients(new Models.FBClients { ModuleId = module.ModuleId, Name = FBClients.Name });
                }
            }
        }

        public Task<List<SearchContent>> GetSearchContentsAsync(PageModule pageModule, DateTime lastIndexedOn)
        {
           var searchContentList = new List<SearchContent>();

           foreach (var FBClients in _FBClientsRepository.GetFBClientss(pageModule.ModuleId))
           {
               if (FBClients.ModifiedOn >= lastIndexedOn)
               {
                   searchContentList.Add(new SearchContent
                   {
                       EntityName = "GIBSFBClients",
                       EntityId = FBClients.FBClientsId.ToString(),
                       Title = FBClients.Name,
                       Body = FBClients.Name,
                       ContentModifiedBy = FBClients.ModifiedBy,
                       ContentModifiedOn = FBClients.ModifiedOn
                   });
               }
           }

           return Task.FromResult(searchContentList);
        }
    }
}
