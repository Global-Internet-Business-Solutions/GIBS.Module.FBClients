using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Oqtane.Enums;
using Oqtane.Infrastructure;
using Oqtane.Models;
using Oqtane.Repository;
using Oqtane.Security;
using Oqtane.Shared;
using GIBS.Module.FBClients.Lists.Repository;

namespace GIBS.Module.FBClients.Lists.Services
{
    public class ServerListsService : IListsService
    {
        private const string ModuleDefinitionName = "GIBS.Module.FBClients.Lists, GIBS.Module.FBClients.Client.Oqtane";
        private readonly IListsRepository _ListsRepository;
        private readonly IModuleRepository _modules;
        private readonly IUserPermissions _userPermissions;
        private readonly ILogManager _logger;
        private readonly IHttpContextAccessor _accessor;
        private readonly Alias _alias;

        public ServerListsService(IListsRepository ListsRepository, IModuleRepository modules, IUserPermissions userPermissions, ITenantManager tenantManager, ILogManager logger, IHttpContextAccessor accessor)
        {
            _ListsRepository = ListsRepository;
            _modules = modules;
            _userPermissions = userPermissions;
            _logger = logger;
            _accessor = accessor;
            _alias = tenantManager.GetAlias();
        }

        private bool CanAccessModule(int moduleId, string permission)
        {
            var module = _modules.GetModule(moduleId);
            return _alias != null && module != null && module.SiteId == _alias.SiteId
                && module.ModuleDefinitionName == ModuleDefinitionName
                && _userPermissions.IsAuthorized(_accessor.HttpContext?.User, _alias.SiteId, EntityNames.Module, moduleId, permission);
        }

        public Task<List<Models.Lists>> GetListssAsync(int ModuleId)
        {
            if (CanAccessModule(ModuleId, PermissionNames.View))
            {
                return Task.FromResult(_ListsRepository.GetListss(ModuleId).ToList());
            }
            else
            {
                _logger.Log(LogLevel.Error, this, LogFunction.Security, "Unauthorized Lists Get Attempt {ModuleId}", ModuleId);
                throw new UnauthorizedAccessException("The selected module is not available for viewing.");
            }
        }

        public Task<List<Models.Lists>> GetListsAsync(string ListName, int SiteId)
        {
            if (_alias == null || SiteId != _alias.SiteId)
            {
                _logger.Log(LogLevel.Error, this, LogFunction.Security, "Unauthorized Lists Name Lookup {ListName} {SiteId}", ListName, SiteId);
                throw new UnauthorizedAccessException("The selected site is not available for viewing.");
            }

            if (string.IsNullOrWhiteSpace(ListName))
            {
                _logger.Log(LogLevel.Error, this, LogFunction.Read, "Invalid Lists Name Lookup {SiteId}", SiteId);
                throw new ValidationException("List name is required.");
            }

            var entries = _ListsRepository.GetLists(ListName, SiteId).ToList();
            var visibleModules = entries.Select(entry => entry.ModuleId).Distinct()
                .Where(moduleId => CanAccessModule(moduleId, PermissionNames.View))
                .ToHashSet();
            return Task.FromResult(entries.Where(entry => visibleModules.Contains(entry.ModuleId)).ToList());
        }

        public Task<List<Models.Lists>> GetChildListsAsync(int parentId, int siteId)
        {
            if (parentId <= 0)
            {
                throw new ValidationException("A positive parent entry ID is required.");
            }

            var parent = _alias?.SiteId == siteId ? _ListsRepository.GetLists(parentId) : null;
            if (parent == null || parent.SiteID != siteId || !CanAccessModule(parent.ModuleId, PermissionNames.View))
            {
                _logger.Log(LogLevel.Error, this, LogFunction.Security, "Unauthorized Lists Child Lookup {ParentId} {SiteId}", parentId, siteId);
                throw new UnauthorizedAccessException("The selected parent entry is not available for viewing.");
            }

            return Task.FromResult(_ListsRepository.GetChildLists(parentId, parent.ModuleId, siteId).ToList());
        }

        public Task<Models.Lists> GetListsAsync(int ListsId, int ModuleId)
        {
            if (CanAccessModule(ModuleId, PermissionNames.View))
            {
                var entry = _ListsRepository.GetLists(ListsId);
                if (entry != null && entry.ModuleId == ModuleId)
                {
                    return Task.FromResult(entry);
                }
            }

            _logger.Log(LogLevel.Error, this, LogFunction.Security, "Unauthorized Lists Get Attempt {ListsId} {ModuleId}", ListsId, ModuleId);
            return Task.FromResult<Models.Lists>(null);
        }

        public Task<Models.Lists> AddListsAsync(Models.Lists Lists, bool createNewList = false)
        {
            if (CanAccessModule(Lists.ModuleId, PermissionNames.Edit))
            {
                Lists.ListName = Lists.ListName?.Trim();
                ValidateEntry(Lists);
                ValidateParent(Lists);
                Lists.SiteID = _alias.SiteId;
                var group = GetGroup(Lists);
                if (createNewList && group.Count > 0)
                {
                    _logger.Log(LogLevel.Error, this, LogFunction.Create, "Duplicate Lists Name {ListName} {ParentID} {ModuleId}", Lists.ListName, Lists.ParentID, Lists.ModuleId);
                    throw new ValidationException("This list already exists for the selected parent.");
                }

                if (group.Count > 0)
                {
                    var template = group.First();
                    Lists.Name = template.Name;
                    Lists.ListName = template.ListName;
                    Lists.Level = template.Level;
                    Lists.DefinitionID = template.DefinitionID;
                    Lists.SystemList = template.SystemList;
                    Lists.SortOrder = group.Any(item => item.SortOrder > 0) ? checked(group.Max(item => item.SortOrder) + 1) : 0;
                }
                else
                {
                    Lists.Name = Lists.ListName;
                    Lists.Level = Lists.ParentID == 0 ? 0 : checked(_ListsRepository.GetLists(Lists.ParentID).Level + 1);
                    Lists.DefinitionID = -1;
                    Lists.SystemList = false;
                    Lists.SortOrder = Lists.SortOrder > 0 ? 1 : 0;
                }

                Lists.ListsId = 0;
                Lists.Description = string.Empty;
                Lists = _ListsRepository.AddLists(Lists);
                _logger.Log(LogLevel.Information, this, LogFunction.Create, "Lists Added {Lists}", Lists);
            }
            else
            {
                _logger.Log(LogLevel.Error, this, LogFunction.Security, "Unauthorized Lists Add Attempt {ModuleId}", Lists.ModuleId);
                throw new UnauthorizedAccessException("The selected module is not available for editing.");
            }
            return Task.FromResult(Lists);
        }

        public Task<Models.Lists> UpdateListsAsync(Models.Lists Lists)
        {
            var existing = RequireEditableEntry(Lists.ListsId, Lists.ModuleId);
            existing.Text = Lists.Text;
            existing.Value = Lists.Value;
            ValidateEntry(existing);
            var updated = _ListsRepository.UpdateLists(existing);
            _logger.Log(LogLevel.Information, this, LogFunction.Update, "Lists Updated {Lists}", updated);
            return Task.FromResult(updated);
        }

        public Task DeleteListsAsync(int ListsId, int ModuleId)
        {
            RequireEditableEntry(ListsId, ModuleId);
            DeleteEntries(new[] { ListsId }, ModuleId);
            _logger.Log(LogLevel.Information, this, LogFunction.Delete, "Lists Entry And Descendants Deleted {ListsId}", ListsId);
            return Task.CompletedTask;
        }

        public Task DeleteListAsync(int ListsId, int ModuleId)
        {
            var entry = RequireEditableEntry(ListsId, ModuleId);
            var ids = GetGroup(entry).Select(item => item.ListsId);
            DeleteEntries(ids, ModuleId);
            _logger.Log(LogLevel.Information, this, LogFunction.Delete, "Lists And Descendants Deleted {ListsId}", ListsId);
            return Task.CompletedTask;
        }

        private void DeleteEntries(IEnumerable<int> roots, int moduleId)
        {
            var entries = _ListsRepository.GetListss(moduleId).ToList();
            var children = entries.ToLookup(item => item.ParentID);
            var ids = new HashSet<int>(roots);
            var pending = new Queue<int>(ids);
            while (pending.Count > 0)
            {
                foreach (var child in children[pending.Dequeue()])
                {
                    if (ids.Add(child.ListsId))
                    {
                        pending.Enqueue(child.ListsId);
                    }
                }
            }

            _ListsRepository.DeleteEntries(ids, moduleId);
        }

        public Task MoveListsAsync(int ListsId, int direction, int ModuleId)
        {
            var entry = RequireEditableEntry(ListsId, ModuleId);
            var entries = GetGroup(entry).OrderBy(item => item.SortOrder)
                .ThenBy(item => item.Text).ThenBy(item => item.ListsId).ToList();
            var index = entries.FindIndex(item => item.ListsId == ListsId);
            var targetIndex = index + direction;
            if ((direction != -1 && direction != 1) || index < 0 || !entries.Any(item => item.SortOrder > 0)
                || targetIndex < 0 || targetIndex >= entries.Count)
            {
                _logger.Log(LogLevel.Error, this, LogFunction.Update, "Invalid Lists Move {ListsId} {Direction}", ListsId, direction);
                throw new ValidationException("The selected entry cannot be moved in that direction.");
            }

            entries.RemoveAt(index);
            entries.Insert(targetIndex, entry);
            for (var order = 0; order < entries.Count; order++)
            {
                entries[order].SortOrder = order + 1;
            }

            _ListsRepository.UpdateSortOrders(entries, ModuleId);
            _logger.Log(LogLevel.Information, this, LogFunction.Update, "Lists Entry Moved {ListsId} {Direction}", ListsId, direction);
            return Task.CompletedTask;
        }

        private List<Models.Lists> GetGroup(Models.Lists entry)
        {
            return _ListsRepository.GetListss(entry.ModuleId)
                .Where(item => item.ParentID == entry.ParentID
                    && string.Equals(item.ListName, entry.ListName, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        private Models.Lists RequireEditableEntry(int listsId, int moduleId)
        {
            var entry = CanAccessModule(moduleId, PermissionNames.Edit) ? _ListsRepository.GetLists(listsId) : null;
            if (entry == null || entry.ModuleId != moduleId)
            {
                _logger.Log(LogLevel.Error, this, LogFunction.Security, "Unauthorized Lists Management Attempt {ListsId} {ModuleId}", listsId, moduleId);
                throw new UnauthorizedAccessException("The selected list entry is not available for editing.");
            }

            return entry;
        }

        private void ValidateEntry(Models.Lists entry)
        {
            try
            {
                Validator.ValidateObject(entry, new ValidationContext(entry), true);
                if (string.IsNullOrWhiteSpace(entry.ListName) || string.IsNullOrWhiteSpace(entry.Text) || string.IsNullOrWhiteSpace(entry.Value))
                {
                    throw new ValidationException("List name, text, and value are required.");
                }
            }
            catch (ValidationException ex)
            {
                _logger.Log(LogLevel.Error, this, LogFunction.Update, ex, "Invalid Lists Entry {ListsId} {ModuleId}", entry.ListsId, entry.ModuleId);
                throw;
            }
        }

        private void ValidateParent(Models.Lists entry)
        {
            var parentId = entry.ParentID;
            var visited = new HashSet<int>();
            if (entry.ListsId > 0)
            {
                visited.Add(entry.ListsId);
            }

            while (parentId != 0)
            {
                var parent = _ListsRepository.GetLists(parentId);
                if (!visited.Add(parentId) || parent == null || parent.ModuleId != entry.ModuleId)
                {
                    _logger.Log(LogLevel.Error, this, LogFunction.Update, "Invalid Lists Parent {ListsId} {ParentID} {ModuleId}", entry.ListsId, entry.ParentID, entry.ModuleId);
                    throw new ValidationException("The selected parent is missing, circular, or belongs to another module.");
                }

                parentId = parent.ParentID;
            }
        }
    }
}
