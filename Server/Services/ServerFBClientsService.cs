using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Oqtane.Enums;
using Oqtane.Infrastructure;
using Oqtane.Models;
using Oqtane.Security;
using Oqtane.Shared;
using GIBS.Module.FBClients.Repository;

namespace GIBS.Module.FBClients.Services
{
    public class ServerFBClientsService : IFBClientsService
    {
        private readonly IFBClientsRepository _FBClientsRepository;
        private readonly IUserPermissions _userPermissions;
        private readonly ILogManager _logger;
        private readonly IHttpContextAccessor _accessor;
        private readonly Alias _alias;

        public ServerFBClientsService(IFBClientsRepository FBClientsRepository, IUserPermissions userPermissions, ITenantManager tenantManager, ILogManager logger, IHttpContextAccessor accessor)
        {
            _FBClientsRepository = FBClientsRepository;
            _userPermissions = userPermissions;
            _logger = logger;
            _accessor = accessor;
            _alias = tenantManager.GetAlias();
        }

        public Task<List<Models.FBClients>> GetFBClientssAsync(int ModuleId)
        {
            if (_userPermissions.IsAuthorized(_accessor.HttpContext.User, _alias.SiteId, EntityNames.Module, ModuleId, PermissionNames.View))
            {
                return Task.FromResult(_FBClientsRepository.GetFBClientss(ModuleId).ToList());
            }
            else
            {
                _logger.Log(LogLevel.Error, this, LogFunction.Security, "Unauthorized FBClients Get Attempt {ModuleId}", ModuleId);
                return null;
            }
        }

        public Task<Models.FBClients> GetFBClientsAsync(int FBClientsId, int ModuleId)
        {
            if (_userPermissions.IsAuthorized(_accessor.HttpContext.User, _alias.SiteId, EntityNames.Module, ModuleId, PermissionNames.View))
            {
                return Task.FromResult(_FBClientsRepository.GetFBClients(FBClientsId));
            }
            else
            {
                _logger.Log(LogLevel.Error, this, LogFunction.Security, "Unauthorized FBClients Get Attempt {FBClientsId} {ModuleId}", FBClientsId, ModuleId);
                return null;
            }
        }

        public Task<Models.FBClients> AddFBClientsAsync(Models.FBClients FBClients)
        {
            if (_userPermissions.IsAuthorized(_accessor.HttpContext.User, _alias.SiteId, EntityNames.Module, FBClients.ModuleId, PermissionNames.Edit))
            {
                FBClients = _FBClientsRepository.AddFBClients(FBClients);
                _logger.Log(LogLevel.Information, this, LogFunction.Create, "FBClients Added {FBClients}", FBClients);
            }
            else
            {
                _logger.Log(LogLevel.Error, this, LogFunction.Security, "Unauthorized FBClients Add Attempt {FBClients}", FBClients);
                FBClients = null;
            }
            return Task.FromResult(FBClients);
        }

        public Task<Models.FBClients> UpdateFBClientsAsync(Models.FBClients FBClients)
        {
            if (_userPermissions.IsAuthorized(_accessor.HttpContext.User, _alias.SiteId, EntityNames.Module, FBClients.ModuleId, PermissionNames.Edit))
            {
                FBClients = _FBClientsRepository.UpdateFBClients(FBClients);
                _logger.Log(LogLevel.Information, this, LogFunction.Update, "FBClients Updated {FBClients}", FBClients);
            }
            else
            {
                _logger.Log(LogLevel.Error, this, LogFunction.Security, "Unauthorized FBClients Update Attempt {FBClients}", FBClients);
                FBClients = null;
            }
            return Task.FromResult(FBClients);
        }

        public Task DeleteFBClientsAsync(int FBClientsId, int ModuleId)
        {
            if (_userPermissions.IsAuthorized(_accessor.HttpContext.User, _alias.SiteId, EntityNames.Module, ModuleId, PermissionNames.Edit))
            {
                _FBClientsRepository.DeleteFBClients(FBClientsId);
                _logger.Log(LogLevel.Information, this, LogFunction.Delete, "FBClients Deleted {FBClientsId}", FBClientsId);
            }
            else
            {
                _logger.Log(LogLevel.Error, this, LogFunction.Security, "Unauthorized FBClients Delete Attempt {FBClientsId} {ModuleId}", FBClientsId, ModuleId);
            }
            return Task.CompletedTask;
        }
    }
}
