using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using GIBS.Module.FBClients.Lists.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Oqtane.Controllers;
using Oqtane.Enums;
using Oqtane.Infrastructure;
using Oqtane.Shared;

namespace GIBS.Module.FBClients.Lists.Controllers
{
    [Route(ControllerRoutes.ApiRoute)]
    public class FBClientsListsController : ModuleControllerBase
    {
        private readonly IListsService _ListsService;

        public FBClientsListsController(IListsService ListsService, ILogManager logger, IHttpContextAccessor accessor) : base(logger, accessor)
        {
            _ListsService = ListsService;
        }

        [HttpGet]
        [Authorize(Policy = PolicyNames.ViewModule)]
        public Task<IActionResult> Get([FromQuery] int moduleid) => InModule(moduleid,
            async () => Ok(await _ListsService.GetListssAsync(moduleid)));

        [HttpGet("by-name")]
        [AllowAnonymous]
        public Task<IActionResult> GetByName([FromQuery] string listName, [FromQuery] int siteid) => Execute(
            async () => Ok(await _ListsService.GetListsAsync(listName, siteid)));

        [HttpGet("children/{parentId:int}")]
        [AllowAnonymous]
        public Task<IActionResult> GetChildren(int parentId, [FromQuery] int siteid) => Execute(
            async () => Ok(await _ListsService.GetChildListsAsync(parentId, siteid)));

        [HttpGet("{id:int}/{moduleid:int}")]
        [Authorize(Policy = PolicyNames.ViewModule)]
        public Task<IActionResult> Get(int id, int moduleid) => InModule(moduleid, async () =>
        {
            var entry = await _ListsService.GetListsAsync(id, moduleid);
            return entry == null ? StatusCode(StatusCodes.Status403Forbidden) : Ok(entry);
        });

        [HttpPost]
        [Authorize(Policy = PolicyNames.EditModule)]
        public Task<IActionResult> Post([FromBody] Models.Lists entry, [FromQuery] bool createNewList = false)
        {
            if (!ModelState.IsValid || entry == null)
            {
                return Task.FromResult<IActionResult>(BadRequest("A valid list entry is required."));
            }

            return InModule(entry.ModuleId, async () => Ok(await _ListsService.AddListsAsync(entry, createNewList)));
        }

        [HttpPut("{id:int}")]
        [Authorize(Policy = PolicyNames.EditModule)]
        public Task<IActionResult> Put(int id, [FromBody] Models.Lists entry)
        {
            if (!ModelState.IsValid || entry == null || entry.ListsId != id)
            {
                return Task.FromResult<IActionResult>(BadRequest("A valid list entry matching the requested ID is required."));
            }

            return InModule(entry.ModuleId, async () => Ok(await _ListsService.UpdateListsAsync(entry)));
        }

        [HttpDelete("list/{id:int}/{moduleid:int}")]
        [Authorize(Policy = PolicyNames.EditModule)]
        public Task<IActionResult> DeleteList(int id, int moduleid) => InModule(moduleid, async () =>
        {
            await _ListsService.DeleteListAsync(id, moduleid);
            return NoContent();
        });

        [HttpPut("move/{id:int}/{direction:int}/{moduleid:int}")]
        [Authorize(Policy = PolicyNames.EditModule)]
        public Task<IActionResult> Move(int id, int direction, int moduleid) => InModule(moduleid, async () =>
        {
            await _ListsService.MoveListsAsync(id, direction, moduleid);
            return NoContent();
        });

        [HttpDelete("{id:int}/{moduleid:int}")]
        [Authorize(Policy = PolicyNames.EditModule)]
        public Task<IActionResult> Delete(int id, int moduleid) => InModule(moduleid, async () =>
        {
            await _ListsService.DeleteListsAsync(id, moduleid);
            return NoContent();
        });

        private Task<IActionResult> InModule(int moduleId, Func<Task<IActionResult>> action)
        {
            if (!IsAuthorizedEntityId(EntityNames.Module, moduleId))
            {
                _logger.Log(LogLevel.Error, this, LogFunction.Security, "Unauthorized Lists Request {ModuleId}", moduleId);
                return Task.FromResult<IActionResult>(StatusCode(StatusCodes.Status403Forbidden));
            }

            return Execute(action);
        }

        private async Task<IActionResult> Execute(Func<Task<IActionResult>> action)
        {
            try
            {
                return await action();
            }
            catch (ValidationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (UnauthorizedAccessException)
            {
                return StatusCode(StatusCodes.Status403Forbidden);
            }
        }
    }
}
