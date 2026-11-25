using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using GIBS.Module.FBClients.Lists.Repository;
using Oqtane.Interfaces;
using Oqtane.Models;
using Oqtane.Modules;

namespace GIBS.Module.FBClients.Lists.Manager
{
    public class ListsManager : IPortable, ISearchable
    {
        private readonly IListsRepository _ListsRepository;

        public ListsManager(IListsRepository ListsRepository)
        {
            _ListsRepository = ListsRepository;
        }

        public string ExportModule(Oqtane.Models.Module module)
        {
            var lists = _ListsRepository.GetListss(module.ModuleId)
                .OrderBy(item => item.Level)
                .ThenBy(item => item.ListsId)
                .ToList();
            return JsonSerializer.Serialize(lists);
        }

        public void ImportModule(Oqtane.Models.Module module, string content, string version)
        {
            if (string.IsNullOrEmpty(content))
            {
                return;
            }

            var sourceLists = JsonSerializer.Deserialize<List<Models.Lists>>(content);
            if (sourceLists == null)
            {
                return;
            }

            var pendingLists = sourceLists.OrderBy(item => item.Level).ThenBy(item => item.ListsId).ToList();
            var idMap = new Dictionary<int, int>();
            while (pendingLists.Count > 0)
            {
                var importableLists = pendingLists
                    .Where(item => item.ParentID == 0 || idMap.ContainsKey(item.ParentID))
                    .ToList();
                if (importableLists.Count == 0)
                {
                    throw new InvalidOperationException("Unable to import Lists because a parent entry is missing or the hierarchy contains a cycle.");
                }

                foreach (var sourceList in importableLists)
                {
                    var importedList = new Models.Lists
                    {
                        ModuleId = module.ModuleId,
                        Name = sourceList.Name,
                        ListName = string.IsNullOrWhiteSpace(sourceList.ListName) ? sourceList.Name : sourceList.ListName,
                        Value = string.IsNullOrEmpty(sourceList.Value) ? sourceList.Name : sourceList.Value,
                        Text = string.IsNullOrEmpty(sourceList.Text) ? sourceList.Name : sourceList.Text,
                        ParentID = sourceList.ParentID == 0 ? 0 : idMap[sourceList.ParentID],
                        Level = sourceList.Level,
                        SortOrder = sourceList.SortOrder,
                        DefinitionID = sourceList.DefinitionID,
                        Description = sourceList.Description,
                        SiteID = module.SiteId,
                        SystemList = sourceList.SystemList
                    };
                    var addedList = _ListsRepository.AddLists(importedList);
                    idMap.Add(sourceList.ListsId, addedList.ListsId);
                    pendingLists.Remove(sourceList);
                }
            }
        }

        public Task<List<SearchContent>> GetSearchContentsAsync(PageModule pageModule, DateTime lastIndexedOn)
        {
           var searchContentList = new List<SearchContent>();

           foreach (var Lists in _ListsRepository.GetListss(pageModule.ModuleId))
           {
               if (Lists.ModifiedOn >= lastIndexedOn)
               {
                   searchContentList.Add(new SearchContent
                   {
                       EntityName = "GIBSFBClientsLists",
                       EntityId = Lists.ListsId.ToString(),
                       Title = Lists.Name,
                       Body = Lists.Name,
                       ContentModifiedBy = Lists.ModifiedBy,
                       ContentModifiedOn = Lists.ModifiedOn
                   });
               }
           }

           return Task.FromResult(searchContentList);
        }
    }
}
