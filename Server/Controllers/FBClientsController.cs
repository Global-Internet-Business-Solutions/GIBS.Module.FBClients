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
                _logger.Log(LogLevel.Error, this, LogFunction.Security, "Unauthorized FBClients Post Attempt {FBClients}", FBClients);
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
                _logger.Log(LogLevel.Error, this, LogFunction.Security, "Unauthorized FBClients Put Attempt {FBClients}", FBClients);
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
    }
}
