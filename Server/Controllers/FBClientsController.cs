using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Oqtane.Shared;
using Oqtane.Enums;
using Oqtane.Infrastructure;
using GIBS.Module.FBClients.Services;
using Oqtane.Controllers;
using System.Net;
using System.Threading.Tasks;

namespace GIBS.Module.FBClients.Controllers
{
    [Route(ControllerRoutes.ApiRoute)]
    public class FBClientsController : ModuleControllerBase
    {
        private readonly IFBClientsService _FBClientsService;

        public FBClientsController(IFBClientsService FBClientsService, ILogManager logger, IHttpContextAccessor accessor) : base(logger, accessor)
        {
            _FBClientsService = FBClientsService;
        }

        // GET: api/<controller>?moduleid=x
        [HttpGet]
        [Authorize(Policy = PolicyNames.ViewModule)]
        public async Task<IEnumerable<Models.FBClients>> Get(string moduleid)
        {
            int ModuleId;
            if (int.TryParse(moduleid, out ModuleId) && IsAuthorizedEntityId(EntityNames.Module, ModuleId))
            {
                return await _FBClientsService.GetFBClientssAsync(ModuleId);
            }
            else
            {
                _logger.Log(LogLevel.Error, this, LogFunction.Security, "Unauthorized FBClients Get Attempt {ModuleId}", moduleid);
                HttpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                return null;
            }
        }

        // GET api/<controller>/5
        [HttpGet("{id}/{moduleid}")]
        [Authorize(Policy = PolicyNames.ViewModule)]
        public async Task<Models.FBClients> Get(int id, int moduleid)
        {
            Models.FBClients FBClients = await _FBClientsService.GetFBClientsAsync(id, moduleid);
            if (FBClients != null && IsAuthorizedEntityId(EntityNames.Module, FBClients.ModuleId))
            {
                return FBClients;
            }
            else
            { 
                _logger.Log(LogLevel.Error, this, LogFunction.Security, "Unauthorized FBClients Get Attempt {FBClientsId} {ModuleId}", id, moduleid);
                HttpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                return null;
            }
        }

        // POST api/<controller>
        [HttpPost]
        [Authorize(Policy = PolicyNames.EditModule)]
        public async Task<Models.FBClients> Post([FromBody] Models.FBClients FBClients)
        {
            if (ModelState.IsValid && IsAuthorizedEntityId(EntityNames.Module, FBClients.ModuleId))
            {
                FBClients = await _FBClientsService.AddFBClientsAsync(FBClients);
            }
            else
            {
                _logger.Log(LogLevel.Error, this, LogFunction.Security, "Unauthorized FBClients Post Attempt {FBClientsId} {ModuleId}", FBClients?.FBClientsId, FBClients?.ModuleId);
                HttpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                FBClients = null;
            }
            return FBClients;
        }

        // PUT api/<controller>/5
        [HttpPut("{id}")]
        [Authorize(Policy = PolicyNames.EditModule)]
        public async Task<Models.FBClients> Put(int id, [FromBody] Models.FBClients FBClients)
        {
            if (ModelState.IsValid && FBClients.FBClientsId == id && IsAuthorizedEntityId(EntityNames.Module, FBClients.ModuleId))
            {
                FBClients = await _FBClientsService.UpdateFBClientsAsync(FBClients);
            }
            else
            {
                _logger.Log(LogLevel.Error, this, LogFunction.Security, "Unauthorized FBClients Put Attempt {FBClientsId} {ModuleId}", FBClients?.FBClientsId, FBClients?.ModuleId);
                HttpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                FBClients = null;
            }
            return FBClients;
        }

        // DELETE api/<controller>/5
        [HttpDelete("{id}/{moduleid}")]
        [Authorize(Policy = PolicyNames.EditModule)]
        public async Task Delete(int id, int moduleid)
        {
            Models.FBClients FBClients = await _FBClientsService.GetFBClientsAsync(id, moduleid);
            if (FBClients != null && IsAuthorizedEntityId(EntityNames.Module, FBClients.ModuleId))
            {
                await _FBClientsService.DeleteFBClientsAsync(id, FBClients.ModuleId);
            }
            else
            {
                _logger.Log(LogLevel.Error, this, LogFunction.Security, "Unauthorized FBClients Delete Attempt {FBClientsId} {ModuleId}", id, moduleid);
                HttpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            }
        }

        // Family Members (AFM)
        [HttpGet("afm/list/{clientid}/{moduleid}")]
        [Authorize(Policy = PolicyNames.ViewModule)]
        public async Task<IEnumerable<Models.FBClientsAddFamMem>> GetAddFamMems(int clientid, int moduleid)
        {
            if (IsAuthorizedEntityId(EntityNames.Module, moduleid))
            {
                return await _FBClientsService.GetFBClientsAddFamMemsAsync(clientid, moduleid);
            }
            HttpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            return null;
        }

        [HttpGet("afm/{id}/{moduleid}")]
        [Authorize(Policy = PolicyNames.ViewModule)]
        public async Task<Models.FBClientsAddFamMem> GetAddFamMem(int id, int moduleid)
        {
            if (IsAuthorizedEntityId(EntityNames.Module, moduleid))
            {
                return await _FBClientsService.GetFBClientsAddFamMemAsync(id, moduleid);
            }
            HttpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            return null;
        }

        [HttpPost("afm/{moduleid}")]
        [Authorize(Policy = PolicyNames.EditModule)]
        public async Task<Models.FBClientsAddFamMem> PostAddFamMem(int moduleid, [FromBody] Models.FBClientsAddFamMem item)
        {
            if (ModelState.IsValid && IsAuthorizedEntityId(EntityNames.Module, moduleid))
            {
                return await _FBClientsService.AddFBClientsAddFamMemAsync(item, moduleid);
            }
            HttpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            return null;
        }

        [HttpPut("afm/{id}/{moduleid}")]
        [Authorize(Policy = PolicyNames.EditModule)]
        public async Task<Models.FBClientsAddFamMem> PutAddFamMem(int id, int moduleid, [FromBody] Models.FBClientsAddFamMem item)
        {
            if (ModelState.IsValid && item.FBClientsAddFamMemId == id && IsAuthorizedEntityId(EntityNames.Module, moduleid))
            {
                return await _FBClientsService.UpdateFBClientsAddFamMemAsync(item, moduleid);
            }
            HttpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            return null;
        }

        [HttpDelete("afm/{id}/{moduleid}")]
        [Authorize(Policy = PolicyNames.EditModule)]
        public async Task DeleteAddFamMem(int id, int moduleid)
        {
            if (IsAuthorizedEntityId(EntityNames.Module, moduleid))
            {
                await _FBClientsService.DeleteFBClientsAddFamMemAsync(id, moduleid);
            }
            else
            {
                HttpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            }
        }

        // Income and Expenses
        [HttpGet("ie/list/{clientid}/{moduleid}")]
        [Authorize(Policy = PolicyNames.ViewModule)]
        public async Task<IEnumerable<Models.FBClientsIncomeAndExpense>> GetIncomeAndExpenses(int clientid, int moduleid)
        {
            if (IsAuthorizedEntityId(EntityNames.Module, moduleid))
            {
                return await _FBClientsService.GetFBClientsIncomeAndExpensesAsync(clientid, moduleid);
            }
            HttpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            return null;
        }

        [HttpGet("ie/{id}/{moduleid}")]
        [Authorize(Policy = PolicyNames.ViewModule)]
        public async Task<Models.FBClientsIncomeAndExpense> GetIncomeAndExpense(int id, int moduleid)
        {
            if (IsAuthorizedEntityId(EntityNames.Module, moduleid))
            {
                return await _FBClientsService.GetFBClientsIncomeAndExpenseAsync(id, moduleid);
            }
            HttpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            return null;
        }

        [HttpPost("ie/{moduleid}")]
        [Authorize(Policy = PolicyNames.EditModule)]
        public async Task<Models.FBClientsIncomeAndExpense> PostIncomeAndExpense(int moduleid, [FromBody] Models.FBClientsIncomeAndExpense item)
        {
            if (ModelState.IsValid && IsAuthorizedEntityId(EntityNames.Module, moduleid))
            {
                return await _FBClientsService.AddFBClientsIncomeAndExpenseAsync(item, moduleid);
            }
            HttpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            return null;
        }

        [HttpPut("ie/{id}/{moduleid}")]
        [Authorize(Policy = PolicyNames.EditModule)]
        public async Task<Models.FBClientsIncomeAndExpense> PutIncomeAndExpense(int id, int moduleid, [FromBody] Models.FBClientsIncomeAndExpense item)
        {
            if (ModelState.IsValid && item.IeId == id && IsAuthorizedEntityId(EntityNames.Module, moduleid))
            {
                return await _FBClientsService.UpdateFBClientsIncomeAndExpenseAsync(item, moduleid);
            }
            HttpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            return null;
        }

        [HttpDelete("ie/{id}/{moduleid}")]
        [Authorize(Policy = PolicyNames.EditModule)]
        public async Task DeleteIncomeAndExpense(int id, int moduleid)
        {
            if (IsAuthorizedEntityId(EntityNames.Module, moduleid))
            {
                await _FBClientsService.DeleteFBClientsIncomeAndExpenseAsync(id, moduleid);
            }
            else
            {
                HttpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            }
        }

        // Visits
        [HttpGet("visit/list/{clientid}/{moduleid}")]
        [Authorize(Policy = PolicyNames.ViewModule)]
        public async Task<IEnumerable<Models.FBClientsVisits>> GetVisits(int clientid, int moduleid)
        {
            if (IsAuthorizedEntityId(EntityNames.Module, moduleid))
            {
                return await _FBClientsService.GetFBClientsVisitsAsync(clientid, moduleid);
            }
            HttpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            return null;
        }

        [HttpGet("visit/{id}/{moduleid}")]
        [Authorize(Policy = PolicyNames.ViewModule)]
        public async Task<Models.FBClientsVisits> GetVisit(int id, int moduleid)
        {
            if (IsAuthorizedEntityId(EntityNames.Module, moduleid))
            {
                return await _FBClientsService.GetFBClientsVisitAsync(id, moduleid);
            }
            HttpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            return null;
        }

        [HttpPost("visit/{moduleid}")]
        [Authorize(Policy = PolicyNames.EditModule)]
        public async Task<Models.FBClientsVisits> PostVisit(int moduleid, [FromBody] Models.FBClientsVisits item)
        {
            if (ModelState.IsValid && IsAuthorizedEntityId(EntityNames.Module, moduleid))
            {
                return await _FBClientsService.AddFBClientsVisitAsync(item, moduleid);
            }
            HttpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            return null;
        }

        [HttpPut("visit/{id}/{moduleid}")]
        [Authorize(Policy = PolicyNames.EditModule)]
        public async Task<Models.FBClientsVisits> PutVisit(int id, int moduleid, [FromBody] Models.FBClientsVisits item)
        {
            if (ModelState.IsValid && item.VisitId == id && IsAuthorizedEntityId(EntityNames.Module, moduleid))
            {
                return await _FBClientsService.UpdateFBClientsVisitAsync(item, moduleid);
            }
            HttpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            return null;
        }

        [HttpDelete("visit/{id}/{moduleid}")]
        [Authorize(Policy = PolicyNames.EditModule)]
        public async Task DeleteVisit(int id, int moduleid)
        {
            if (IsAuthorizedEntityId(EntityNames.Module, moduleid))
            {
                await _FBClientsService.DeleteFBClientsVisitAsync(id, moduleid);
            }
            else
            {
                HttpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            }
        }

        // Christmas Toys
        [HttpGet("christmas/list/{clientid}/{moduleid}")]
        [Authorize(Policy = PolicyNames.ViewModule)]
        public async Task<IEnumerable<Models.FBClientsChristmas>> GetChristmases(int clientid, int moduleid)
        {
            if (IsAuthorizedEntityId(EntityNames.Module, moduleid))
            {
                return await _FBClientsService.GetFBClientsChristmasesAsync(clientid, moduleid);
            }
            HttpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            return null;
        }

        [HttpGet("christmas/{id}/{moduleid}")]
        [Authorize(Policy = PolicyNames.ViewModule)]
        public async Task<Models.FBClientsChristmas> GetChristmas(int id, int moduleid)
        {
            if (IsAuthorizedEntityId(EntityNames.Module, moduleid))
            {
                return await _FBClientsService.GetFBClientsChristmasAsync(id, moduleid);
            }
            HttpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            return null;
        }

        [HttpPost("christmas/{moduleid}")]
        [Authorize(Policy = PolicyNames.EditModule)]
        public async Task<Models.FBClientsChristmas> PostChristmas(int moduleid, [FromBody] Models.FBClientsChristmas item)
        {
            if (ModelState.IsValid && IsAuthorizedEntityId(EntityNames.Module, moduleid))
            {
                return await _FBClientsService.AddFBClientsChristmasAsync(item, moduleid);
            }
            HttpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            return null;
        }

        [HttpPut("christmas/{id}/{moduleid}")]
        [Authorize(Policy = PolicyNames.EditModule)]
        public async Task<Models.FBClientsChristmas> PutChristmas(int id, int moduleid, [FromBody] Models.FBClientsChristmas item)
        {
            if (ModelState.IsValid && item.xMasId == id && IsAuthorizedEntityId(EntityNames.Module, moduleid))
            {
                return await _FBClientsService.UpdateFBClientsChristmasAsync(item, moduleid);
            }
            HttpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            return null;
        }

        [HttpDelete("christmas/{id}/{moduleid}")]
        [Authorize(Policy = PolicyNames.EditModule)]
        public async Task DeleteChristmas(int id, int moduleid)
        {
            if (IsAuthorizedEntityId(EntityNames.Module, moduleid))
            {
                await _FBClientsService.DeleteFBClientsChristmasAsync(id, moduleid);
            }
            else
            {
                HttpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            }
        }

        // True/False Questions
        [HttpGet("tf/list/{clientid}/{moduleid}")]
        [Authorize(Policy = PolicyNames.ViewModule)]
        public async Task<IEnumerable<Models.FBClientsTrueFalseQuestions>> GetTrueFalseQuestions(int clientid, int moduleid)
        {
            if (IsAuthorizedEntityId(EntityNames.Module, moduleid))
            {
                return await _FBClientsService.GetFBClientsTrueFalseQuestionsAsync(clientid, moduleid);
            }
            HttpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            return null;
        }

        [HttpPost("tf/{moduleid}")]
        [Authorize(Policy = PolicyNames.EditModule)]
        public async Task<Models.FBClientsTrueFalseQuestions> PostTrueFalseQuestion(int moduleid, [FromBody] Models.FBClientsTrueFalseQuestions item)
        {
            if (ModelState.IsValid && IsAuthorizedEntityId(EntityNames.Module, moduleid))
            {
                return await _FBClientsService.SaveFBClientsTrueFalseQuestionAsync(item, moduleid);
            }
            HttpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            return null;
        }

        // ID Photos
        [HttpGet("photo/list/{clientid}/{moduleid}")]
        [Authorize(Policy = PolicyNames.ViewModule)]
        public async Task<IEnumerable<Models.FBClientsIDPhoto>> GetIDPhotos(int clientid, int moduleid)
        {
            if (IsAuthorizedEntityId(EntityNames.Module, moduleid))
            {
                return await _FBClientsService.GetFBClientsIDPhotosAsync(clientid, moduleid);
            }
            HttpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            return null;
        }

        [HttpPost("photo/{moduleid}")]
        [Authorize(Policy = PolicyNames.EditModule)]
        public async Task<Models.FBClientsIDPhoto> PostIDPhoto(int moduleid, [FromBody] Models.FBClientsIDPhoto item)
        {
            if (ModelState.IsValid && IsAuthorizedEntityId(EntityNames.Module, moduleid))
            {
                return await _FBClientsService.SaveFBClientsIDPhotoAsync(item, moduleid);
            }
            HttpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            return null;
        }

        // Search
        [HttpPost("search/{moduleid}")]
        [Authorize(Policy = PolicyNames.ViewModule)]
        public async Task<IEnumerable<Models.FBClientsSearchResult>> PostSearch(int moduleid, [FromBody] Models.FBClientsSearchRequest request)
        {
            if (request != null && request.ModuleId == moduleid && IsAuthorizedEntityId(EntityNames.Module, moduleid))
            {
                return await _FBClientsService.SearchFBClientsAsync(request);
            }
            HttpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            return null;
        }
    }
}
