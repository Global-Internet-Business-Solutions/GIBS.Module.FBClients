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

        private bool IsClientInModule(int fbClientsId, int moduleId)
        {
            var client = _FBClientsRepository.GetFBClients(fbClientsId, false);
            return client != null && client.ModuleId == moduleId;
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
                _logger.Log(LogLevel.Information, this, LogFunction.Create, "FBClients Added {FBClientsId} {ModuleId}", FBClients.FBClientsId, FBClients.ModuleId);
            }
            else
            {
                _logger.Log(LogLevel.Error, this, LogFunction.Security, "Unauthorized FBClients Add Attempt {FBClientsId} {ModuleId}", FBClients.FBClientsId, FBClients.ModuleId);
                FBClients = null;
            }
            return Task.FromResult(FBClients);
        }

        public Task<Models.FBClients> UpdateFBClientsAsync(Models.FBClients FBClients)
        {
            if (_userPermissions.IsAuthorized(_accessor.HttpContext.User, _alias.SiteId, EntityNames.Module, FBClients.ModuleId, PermissionNames.Edit))
            {
                FBClients = _FBClientsRepository.UpdateFBClients(FBClients);
                _logger.Log(LogLevel.Information, this, LogFunction.Update, "FBClients Updated {FBClientsId} {ModuleId}", FBClients.FBClientsId, FBClients.ModuleId);
            }
            else
            {
                _logger.Log(LogLevel.Error, this, LogFunction.Security, "Unauthorized FBClients Update Attempt {FBClientsId} {ModuleId}", FBClients.FBClientsId, FBClients.ModuleId);
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

        public Task<List<Models.FBClientsAddFamMem>> GetFBClientsAddFamMemsAsync(int FBClientsId, int ModuleId)
        {
            if (_userPermissions.IsAuthorized(_accessor.HttpContext.User, _alias.SiteId, EntityNames.Module, ModuleId, PermissionNames.View) && IsClientInModule(FBClientsId, ModuleId))
            {
                return Task.FromResult(_FBClientsRepository.GetFBClientsAddFamMems(FBClientsId).ToList());
            }
            _logger.Log(LogLevel.Error, this, LogFunction.Security, "Unauthorized FBClientsAddFamMem Get Attempt {FBClientsId} {ModuleId}", FBClientsId, ModuleId);
            return Task.FromResult<List<Models.FBClientsAddFamMem>>(null);
        }

        public Task<Models.FBClientsAddFamMem> GetFBClientsAddFamMemAsync(int FBClientsAddFamMemId, int ModuleId)
        {
            if (_userPermissions.IsAuthorized(_accessor.HttpContext.User, _alias.SiteId, EntityNames.Module, ModuleId, PermissionNames.View))
            {
                var item = _FBClientsRepository.GetFBClientsAddFamMem(FBClientsAddFamMemId, false);
                if (item != null && IsClientInModule(item.FBClientsId, ModuleId))
                {
                    return Task.FromResult(item);
                }
            }
            _logger.Log(LogLevel.Error, this, LogFunction.Security, "Unauthorized FBClientsAddFamMem Get Attempt {FBClientsAddFamMemId} {ModuleId}", FBClientsAddFamMemId, ModuleId);
            return Task.FromResult<Models.FBClientsAddFamMem>(null);
        }

        public Task<Models.FBClientsAddFamMem> AddFBClientsAddFamMemAsync(Models.FBClientsAddFamMem FBClientsAddFamMem, int ModuleId)
        {
            if (_userPermissions.IsAuthorized(_accessor.HttpContext.User, _alias.SiteId, EntityNames.Module, ModuleId, PermissionNames.Edit) && IsClientInModule(FBClientsAddFamMem.FBClientsId, ModuleId))
            {
                FBClientsAddFamMem = _FBClientsRepository.AddFBClientsAddFamMem(FBClientsAddFamMem);
                _logger.Log(LogLevel.Information, this, LogFunction.Create, "FBClientsAddFamMem Added {FBClientsAddFamMemId} {FBClientsId} {ModuleId}", FBClientsAddFamMem.FBClientsAddFamMemId, FBClientsAddFamMem.FBClientsId, ModuleId);
                return Task.FromResult(FBClientsAddFamMem);
            }
            _logger.Log(LogLevel.Error, this, LogFunction.Security, "Unauthorized FBClientsAddFamMem Add Attempt {FBClientsId} {ModuleId}", FBClientsAddFamMem?.FBClientsId, ModuleId);
            return Task.FromResult<Models.FBClientsAddFamMem>(null);
        }

        public Task<Models.FBClientsAddFamMem> UpdateFBClientsAddFamMemAsync(Models.FBClientsAddFamMem FBClientsAddFamMem, int ModuleId)
        {
            if (_userPermissions.IsAuthorized(_accessor.HttpContext.User, _alias.SiteId, EntityNames.Module, ModuleId, PermissionNames.Edit) && IsClientInModule(FBClientsAddFamMem.FBClientsId, ModuleId))
            {
                FBClientsAddFamMem = _FBClientsRepository.UpdateFBClientsAddFamMem(FBClientsAddFamMem);
                _logger.Log(LogLevel.Information, this, LogFunction.Update, "FBClientsAddFamMem Updated {FBClientsAddFamMemId} {FBClientsId} {ModuleId}", FBClientsAddFamMem.FBClientsAddFamMemId, FBClientsAddFamMem.FBClientsId, ModuleId);
                return Task.FromResult(FBClientsAddFamMem);
            }
            _logger.Log(LogLevel.Error, this, LogFunction.Security, "Unauthorized FBClientsAddFamMem Update Attempt {FBClientsAddFamMemId} {ModuleId}", FBClientsAddFamMem?.FBClientsAddFamMemId, ModuleId);
            return Task.FromResult<Models.FBClientsAddFamMem>(null);
        }

        public Task DeleteFBClientsAddFamMemAsync(int FBClientsAddFamMemId, int ModuleId)
        {
            var existing = _FBClientsRepository.GetFBClientsAddFamMem(FBClientsAddFamMemId, false);
            if (_userPermissions.IsAuthorized(_accessor.HttpContext.User, _alias.SiteId, EntityNames.Module, ModuleId, PermissionNames.Edit) && existing != null && IsClientInModule(existing.FBClientsId, ModuleId))
            {
                _FBClientsRepository.DeleteFBClientsAddFamMem(FBClientsAddFamMemId);
                _logger.Log(LogLevel.Information, this, LogFunction.Delete, "FBClientsAddFamMem Deleted {FBClientsAddFamMemId} {ModuleId}", FBClientsAddFamMemId, ModuleId);
            }
            else
            {
                _logger.Log(LogLevel.Error, this, LogFunction.Security, "Unauthorized FBClientsAddFamMem Delete Attempt {FBClientsAddFamMemId} {ModuleId}", FBClientsAddFamMemId, ModuleId);
            }
            return Task.CompletedTask;
        }

        public Task<List<Models.FBClientsIncomeAndExpense>> GetFBClientsIncomeAndExpensesAsync(int FBClientsId, int ModuleId)
        {
            if (_userPermissions.IsAuthorized(_accessor.HttpContext.User, _alias.SiteId, EntityNames.Module, ModuleId, PermissionNames.View) && IsClientInModule(FBClientsId, ModuleId))
            {
                return Task.FromResult(_FBClientsRepository.GetFBClientsIncomeAndExpenses(FBClientsId).ToList());
            }
            _logger.Log(LogLevel.Error, this, LogFunction.Security, "Unauthorized FBClientsIncomeAndExpense Get Attempt {FBClientsId} {ModuleId}", FBClientsId, ModuleId);
            return Task.FromResult<List<Models.FBClientsIncomeAndExpense>>(null);
        }

        public Task<Models.FBClientsIncomeAndExpense> GetFBClientsIncomeAndExpenseAsync(int IeId, int ModuleId)
        {
            if (_userPermissions.IsAuthorized(_accessor.HttpContext.User, _alias.SiteId, EntityNames.Module, ModuleId, PermissionNames.View))
            {
                var item = _FBClientsRepository.GetFBClientsIncomeAndExpense(IeId, false);
                if (item != null && IsClientInModule(item.FBClientsId, ModuleId))
                {
                    return Task.FromResult(item);
                }
            }
            _logger.Log(LogLevel.Error, this, LogFunction.Security, "Unauthorized FBClientsIncomeAndExpense Get Attempt {IeId} {ModuleId}", IeId, ModuleId);
            return Task.FromResult<Models.FBClientsIncomeAndExpense>(null);
        }

        public Task<Models.FBClientsIncomeAndExpense> AddFBClientsIncomeAndExpenseAsync(Models.FBClientsIncomeAndExpense FBClientsIncomeAndExpense, int ModuleId)
        {
            if (_userPermissions.IsAuthorized(_accessor.HttpContext.User, _alias.SiteId, EntityNames.Module, ModuleId, PermissionNames.Edit) && IsClientInModule(FBClientsIncomeAndExpense.FBClientsId, ModuleId))
            {
                FBClientsIncomeAndExpense = _FBClientsRepository.AddFBClientsIncomeAndExpense(FBClientsIncomeAndExpense);
                _logger.Log(LogLevel.Information, this, LogFunction.Create, "FBClientsIncomeAndExpense Added {IeId} {FBClientsId} {ModuleId}", FBClientsIncomeAndExpense.IeId, FBClientsIncomeAndExpense.FBClientsId, ModuleId);
                return Task.FromResult(FBClientsIncomeAndExpense);
            }
            _logger.Log(LogLevel.Error, this, LogFunction.Security, "Unauthorized FBClientsIncomeAndExpense Add Attempt {FBClientsId} {ModuleId}", FBClientsIncomeAndExpense?.FBClientsId, ModuleId);
            return Task.FromResult<Models.FBClientsIncomeAndExpense>(null);
        }

        public Task<Models.FBClientsIncomeAndExpense> UpdateFBClientsIncomeAndExpenseAsync(Models.FBClientsIncomeAndExpense FBClientsIncomeAndExpense, int ModuleId)
        {
            if (_userPermissions.IsAuthorized(_accessor.HttpContext.User, _alias.SiteId, EntityNames.Module, ModuleId, PermissionNames.Edit) && IsClientInModule(FBClientsIncomeAndExpense.FBClientsId, ModuleId))
            {
                FBClientsIncomeAndExpense = _FBClientsRepository.UpdateFBClientsIncomeAndExpense(FBClientsIncomeAndExpense);
                _logger.Log(LogLevel.Information, this, LogFunction.Update, "FBClientsIncomeAndExpense Updated {IeId} {FBClientsId} {ModuleId}", FBClientsIncomeAndExpense.IeId, FBClientsIncomeAndExpense.FBClientsId, ModuleId);
                return Task.FromResult(FBClientsIncomeAndExpense);
            }
            _logger.Log(LogLevel.Error, this, LogFunction.Security, "Unauthorized FBClientsIncomeAndExpense Update Attempt {IeId} {ModuleId}", FBClientsIncomeAndExpense?.IeId, ModuleId);
            return Task.FromResult<Models.FBClientsIncomeAndExpense>(null);
        }

        public Task DeleteFBClientsIncomeAndExpenseAsync(int IeId, int ModuleId)
        {
            var existing = _FBClientsRepository.GetFBClientsIncomeAndExpense(IeId, false);
            if (_userPermissions.IsAuthorized(_accessor.HttpContext.User, _alias.SiteId, EntityNames.Module, ModuleId, PermissionNames.Edit) && existing != null && IsClientInModule(existing.FBClientsId, ModuleId))
            {
                _FBClientsRepository.DeleteFBClientsIncomeAndExpense(IeId);
                _logger.Log(LogLevel.Information, this, LogFunction.Delete, "FBClientsIncomeAndExpense Deleted {IeId} {ModuleId}", IeId, ModuleId);
            }
            else
            {
                _logger.Log(LogLevel.Error, this, LogFunction.Security, "Unauthorized FBClientsIncomeAndExpense Delete Attempt {IeId} {ModuleId}", IeId, ModuleId);
            }
            return Task.CompletedTask;
        }

        public Task<List<Models.FBClientsVisits>> GetFBClientsVisitsAsync(int FBClientsId, int ModuleId)
        {
            if (_userPermissions.IsAuthorized(_accessor.HttpContext.User, _alias.SiteId, EntityNames.Module, ModuleId, PermissionNames.View) && IsClientInModule(FBClientsId, ModuleId))
            {
                return Task.FromResult(_FBClientsRepository.GetFBClientsVisits(FBClientsId).ToList());
            }
            _logger.Log(LogLevel.Error, this, LogFunction.Security, "Unauthorized FBClientsVisits Get Attempt {FBClientsId} {ModuleId}", FBClientsId, ModuleId);
            return Task.FromResult<List<Models.FBClientsVisits>>(null);
        }

        public Task<Models.FBClientsVisits> GetFBClientsVisitAsync(int VisitId, int ModuleId)
        {
            if (_userPermissions.IsAuthorized(_accessor.HttpContext.User, _alias.SiteId, EntityNames.Module, ModuleId, PermissionNames.View))
            {
                var item = _FBClientsRepository.GetFBClientsVisit(VisitId, false);
                if (item != null && IsClientInModule(item.FBClientsId, ModuleId))
                {
                    return Task.FromResult(item);
                }
            }
            _logger.Log(LogLevel.Error, this, LogFunction.Security, "Unauthorized FBClientsVisits Get Attempt {VisitId} {ModuleId}", VisitId, ModuleId);
            return Task.FromResult<Models.FBClientsVisits>(null);
        }

        public Task<Models.FBClientsVisits> AddFBClientsVisitAsync(Models.FBClientsVisits FBClientsVisit, int ModuleId)
        {
            if (_userPermissions.IsAuthorized(_accessor.HttpContext.User, _alias.SiteId, EntityNames.Module, ModuleId, PermissionNames.Edit) && IsClientInModule(FBClientsVisit.FBClientsId, ModuleId))
            {
                FBClientsVisit = _FBClientsRepository.AddFBClientsVisit(FBClientsVisit);
                _logger.Log(LogLevel.Information, this, LogFunction.Create, "FBClientsVisits Added {VisitId} {FBClientsId} {ModuleId}", FBClientsVisit.VisitId, FBClientsVisit.FBClientsId, ModuleId);
                return Task.FromResult(FBClientsVisit);
            }
            _logger.Log(LogLevel.Error, this, LogFunction.Security, "Unauthorized FBClientsVisits Add Attempt {FBClientsId} {ModuleId}", FBClientsVisit?.FBClientsId, ModuleId);
            return Task.FromResult<Models.FBClientsVisits>(null);
        }

        public Task<Models.FBClientsVisits> UpdateFBClientsVisitAsync(Models.FBClientsVisits FBClientsVisit, int ModuleId)
        {
            if (_userPermissions.IsAuthorized(_accessor.HttpContext.User, _alias.SiteId, EntityNames.Module, ModuleId, PermissionNames.Edit) && IsClientInModule(FBClientsVisit.FBClientsId, ModuleId))
            {
                FBClientsVisit = _FBClientsRepository.UpdateFBClientsVisit(FBClientsVisit);
                _logger.Log(LogLevel.Information, this, LogFunction.Update, "FBClientsVisits Updated {VisitId} {FBClientsId} {ModuleId}", FBClientsVisit.VisitId, FBClientsVisit.FBClientsId, ModuleId);
                return Task.FromResult(FBClientsVisit);
            }
            _logger.Log(LogLevel.Error, this, LogFunction.Security, "Unauthorized FBClientsVisits Update Attempt {VisitId} {ModuleId}", FBClientsVisit?.VisitId, ModuleId);
            return Task.FromResult<Models.FBClientsVisits>(null);
        }

        public Task DeleteFBClientsVisitAsync(int VisitId, int ModuleId)
        {
            var existing = _FBClientsRepository.GetFBClientsVisit(VisitId, false);
            if (_userPermissions.IsAuthorized(_accessor.HttpContext.User, _alias.SiteId, EntityNames.Module, ModuleId, PermissionNames.Edit) && existing != null && IsClientInModule(existing.FBClientsId, ModuleId))
            {
                _FBClientsRepository.DeleteFBClientsVisit(VisitId);
                _logger.Log(LogLevel.Information, this, LogFunction.Delete, "FBClientsVisits Deleted {VisitId} {ModuleId}", VisitId, ModuleId);
            }
            else
            {
                _logger.Log(LogLevel.Error, this, LogFunction.Security, "Unauthorized FBClientsVisits Delete Attempt {VisitId} {ModuleId}", VisitId, ModuleId);
            }
            return Task.CompletedTask;
        }

        public Task<List<Models.FBClientsChristmas>> GetFBClientsChristmasesAsync(int FBClientsId, int ModuleId)
        {
            if (_userPermissions.IsAuthorized(_accessor.HttpContext.User, _alias.SiteId, EntityNames.Module, ModuleId, PermissionNames.View) && IsClientInModule(FBClientsId, ModuleId))
            {
                return Task.FromResult(_FBClientsRepository.GetFBClientsChristmases(FBClientsId).ToList());
            }
            _logger.Log(LogLevel.Error, this, LogFunction.Security, "Unauthorized FBClientsChristmas Get Attempt {FBClientsId} {ModuleId}", FBClientsId, ModuleId);
            return Task.FromResult<List<Models.FBClientsChristmas>>(null);
        }

        public Task<Models.FBClientsChristmas> GetFBClientsChristmasAsync(int xMasId, int ModuleId)
        {
            if (_userPermissions.IsAuthorized(_accessor.HttpContext.User, _alias.SiteId, EntityNames.Module, ModuleId, PermissionNames.View))
            {
                var item = _FBClientsRepository.GetFBClientsChristmas(xMasId, false);
                if (item != null && IsClientInModule(item.FBClientsId, ModuleId))
                {
                    return Task.FromResult(item);
                }
            }
            _logger.Log(LogLevel.Error, this, LogFunction.Security, "Unauthorized FBClientsChristmas Get Attempt {xMasId} {ModuleId}", xMasId, ModuleId);
            return Task.FromResult<Models.FBClientsChristmas>(null);
        }

        public Task<Models.FBClientsChristmas> AddFBClientsChristmasAsync(Models.FBClientsChristmas FBClientsChristmas, int ModuleId)
        {
            if (_userPermissions.IsAuthorized(_accessor.HttpContext.User, _alias.SiteId, EntityNames.Module, ModuleId, PermissionNames.Edit) && IsClientInModule(FBClientsChristmas.FBClientsId, ModuleId))
            {
                FBClientsChristmas = _FBClientsRepository.AddFBClientsChristmas(FBClientsChristmas);
                _logger.Log(LogLevel.Information, this, LogFunction.Create, "FBClientsChristmas Added {xMasId} {FBClientsId} {ModuleId}", FBClientsChristmas.xMasId, FBClientsChristmas.FBClientsId, ModuleId);
                return Task.FromResult(FBClientsChristmas);
            }
            _logger.Log(LogLevel.Error, this, LogFunction.Security, "Unauthorized FBClientsChristmas Add Attempt {FBClientsId} {ModuleId}", FBClientsChristmas?.FBClientsId, ModuleId);
            return Task.FromResult<Models.FBClientsChristmas>(null);
        }

        public Task<Models.FBClientsChristmas> UpdateFBClientsChristmasAsync(Models.FBClientsChristmas FBClientsChristmas, int ModuleId)
        {
            if (_userPermissions.IsAuthorized(_accessor.HttpContext.User, _alias.SiteId, EntityNames.Module, ModuleId, PermissionNames.Edit) && IsClientInModule(FBClientsChristmas.FBClientsId, ModuleId))
            {
                FBClientsChristmas = _FBClientsRepository.UpdateFBClientsChristmas(FBClientsChristmas);
                _logger.Log(LogLevel.Information, this, LogFunction.Update, "FBClientsChristmas Updated {xMasId} {FBClientsId} {ModuleId}", FBClientsChristmas.xMasId, FBClientsChristmas.FBClientsId, ModuleId);
                return Task.FromResult(FBClientsChristmas);
            }
            _logger.Log(LogLevel.Error, this, LogFunction.Security, "Unauthorized FBClientsChristmas Update Attempt {xMasId} {ModuleId}", FBClientsChristmas?.xMasId, ModuleId);
            return Task.FromResult<Models.FBClientsChristmas>(null);
        }

        public Task DeleteFBClientsChristmasAsync(int xMasId, int ModuleId)
        {
            var existing = _FBClientsRepository.GetFBClientsChristmas(xMasId, false);
            if (_userPermissions.IsAuthorized(_accessor.HttpContext.User, _alias.SiteId, EntityNames.Module, ModuleId, PermissionNames.Edit) && existing != null && IsClientInModule(existing.FBClientsId, ModuleId))
            {
                _FBClientsRepository.DeleteFBClientsChristmas(xMasId);
                _logger.Log(LogLevel.Information, this, LogFunction.Delete, "FBClientsChristmas Deleted {xMasId} {ModuleId}", xMasId, ModuleId);
            }
            else
            {
                _logger.Log(LogLevel.Error, this, LogFunction.Security, "Unauthorized FBClientsChristmas Delete Attempt {xMasId} {ModuleId}", xMasId, ModuleId);
            }
            return Task.CompletedTask;
        }

        public Task<List<Models.FBClientsTrueFalseQuestions>> GetFBClientsTrueFalseQuestionsAsync(int FBClientsId, int ModuleId)
        {
            if (_userPermissions.IsAuthorized(_accessor.HttpContext.User, _alias.SiteId, EntityNames.Module, ModuleId, PermissionNames.View) && IsClientInModule(FBClientsId, ModuleId))
            {
                return Task.FromResult(_FBClientsRepository.GetFBClientsTrueFalseQuestions(FBClientsId).ToList());
            }
            _logger.Log(LogLevel.Error, this, LogFunction.Security, "Unauthorized FBClientsTrueFalseQuestions Get Attempt {FBClientsId} {ModuleId}", FBClientsId, ModuleId);
            return Task.FromResult<List<Models.FBClientsTrueFalseQuestions>>(null);
        }

        public Task<Models.FBClientsTrueFalseQuestions> SaveFBClientsTrueFalseQuestionAsync(Models.FBClientsTrueFalseQuestions FBClientsTrueFalseQuestion, int ModuleId)
        {
            if (_userPermissions.IsAuthorized(_accessor.HttpContext.User, _alias.SiteId, EntityNames.Module, ModuleId, PermissionNames.Edit) && IsClientInModule(FBClientsTrueFalseQuestion.FBClientsId, ModuleId))
            {
                FBClientsTrueFalseQuestion = _FBClientsRepository.SaveFBClientsTrueFalseQuestion(FBClientsTrueFalseQuestion);
                _logger.Log(LogLevel.Information, this, LogFunction.Update, "FBClientsTrueFalseQuestions Saved {TfId} {FBClientsId} {ModuleId}", FBClientsTrueFalseQuestion.TfId, FBClientsTrueFalseQuestion.FBClientsId, ModuleId);
                return Task.FromResult(FBClientsTrueFalseQuestion);
            }
            _logger.Log(LogLevel.Error, this, LogFunction.Security, "Unauthorized FBClientsTrueFalseQuestions Save Attempt {FBClientsId} {ModuleId}", FBClientsTrueFalseQuestion?.FBClientsId, ModuleId);
            return Task.FromResult<Models.FBClientsTrueFalseQuestions>(null);
        }

        public Task<List<Models.FBClientsIDPhoto>> GetFBClientsIDPhotosAsync(int FBClientsId, int ModuleId)
        {
            if (_userPermissions.IsAuthorized(_accessor.HttpContext.User, _alias.SiteId, EntityNames.Module, ModuleId, PermissionNames.View) && IsClientInModule(FBClientsId, ModuleId))
            {
                return Task.FromResult(_FBClientsRepository.GetFBClientsIDPhotos(FBClientsId).ToList());
            }
            _logger.Log(LogLevel.Error, this, LogFunction.Security, "Unauthorized FBClientsIDPhoto Get Attempt {FBClientsId} {ModuleId}", FBClientsId, ModuleId);
            return Task.FromResult<List<Models.FBClientsIDPhoto>>(null);
        }

        public Task<Models.FBClientsIDPhoto> SaveFBClientsIDPhotoAsync(Models.FBClientsIDPhoto FBClientsIDPhoto, int ModuleId)
        {
            if (FBClientsIDPhoto != null && _userPermissions.IsAuthorized(_accessor.HttpContext.User, _alias.SiteId, EntityNames.Module, ModuleId, PermissionNames.Edit) && IsClientInModule(FBClientsIDPhoto.FBClientsId, ModuleId))
            {
                FBClientsIDPhoto = _FBClientsRepository.SaveFBClientsIDPhoto(FBClientsIDPhoto);
                _logger.Log(LogLevel.Information, this, LogFunction.Update, "FBClientsIDPhoto Saved {ImageId} {FBClientsId} {ModuleId}", FBClientsIDPhoto.ImageId, FBClientsIDPhoto.FBClientsId, ModuleId);
                return Task.FromResult(FBClientsIDPhoto);
            }
            _logger.Log(LogLevel.Error, this, LogFunction.Security, "Unauthorized FBClientsIDPhoto Save Attempt {FBClientsId} {ModuleId}", FBClientsIDPhoto?.FBClientsId, ModuleId);
            return Task.FromResult<Models.FBClientsIDPhoto>(null);
        }

        public Task<List<Models.FBClientsSearchResult>> SearchFBClientsAsync(Models.FBClientsSearchRequest request)
        {
            if (request != null && _userPermissions.IsAuthorized(_accessor.HttpContext.User, _alias.SiteId, EntityNames.Module, request.ModuleId, PermissionNames.View))
            {
                return Task.FromResult(_FBClientsRepository.SearchFBClients(request).ToList());
            }
            _logger.Log(LogLevel.Error, this, LogFunction.Security, "Unauthorized FBClients Search Attempt {ModuleId}", request?.ModuleId);
            return Task.FromResult<List<Models.FBClientsSearchResult>>(null);
        }
    }
}
