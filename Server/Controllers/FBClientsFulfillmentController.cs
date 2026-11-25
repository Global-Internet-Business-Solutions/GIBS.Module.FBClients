using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Oqtane.Controllers;
using Oqtane.Infrastructure;
using Oqtane.Shared;

namespace GIBS.Module.FBClients.Fulfillment
{
    [Route(ControllerRoutes.ApiRoute)]
    [Authorize]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public class FBClientsFulfillmentController(IFulfillmentService service, ILogManager logger, IHttpContextAccessor accessor)
        : ModuleControllerBase(logger, accessor)
    {
        [HttpGet("{moduleId:int}/queue"), Authorize(Policy = PolicyNames.ViewModule)]
        public Task<IActionResult> Queue(int moduleId, [FromQuery] DateOnly date, [FromQuery] int? status, [FromQuery] string location, [FromQuery] int page) =>
            Execute(moduleId, async () => Ok(await service.GetQueueAsync(moduleId, date, status, location, page)));

        [HttpGet("{moduleId:int}/orders/{visitId:int}"), Authorize(Policy = PolicyNames.ViewModule)]
        public Task<IActionResult> Order(int moduleId, int visitId) => Execute(moduleId, async () => Ok(await service.GetOrderAsync(moduleId, visitId)));

        [HttpPost("{moduleId:int}/orders/{visitId:int}/paper"), Authorize(Policy = PolicyNames.EditModule)]
        public Task<IActionResult> Paper(int moduleId, int visitId, [FromBody] Dictionary<int, int> quantities) => Execute(moduleId, async () =>
        {
            await service.SubmitPaperOrderAsync(moduleId, visitId, quantities);
            return NoContent();
        });

        [HttpPost("{moduleId:int}/orders/{visitId:int}/filled"), Authorize(Policy = PolicyNames.EditModule)]
        public Task<IActionResult> Filled(int moduleId, int visitId, [FromBody] int[] checkedItems) => Execute(moduleId, async () =>
        {
            await service.MarkFilledAsync(moduleId, visitId, checkedItems);
            return NoContent();
        });

        [HttpPost("{moduleId:int}/orders/{visitId:int}/pickup-text"), Authorize(Policy = PolicyNames.EditModule)]
        public Task<IActionResult> Text(int moduleId, int visitId) => Execute(moduleId, async () => Ok(await service.SendPickupTextAsync(moduleId, visitId)));

        private async Task<IActionResult> Execute(int moduleId, Func<Task<IActionResult>> action)
        {
            if (!IsAuthorizedEntityId(EntityNames.Module, moduleId)) return Forbid();
            if (!ModelState.IsValid) return ValidationProblem(ModelState);
            try { return await action(); }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (ValidationException ex) { return Problem(ex.Message, statusCode: StatusCodes.Status400BadRequest); }
            catch (KeyNotFoundException ex) { return Problem(ex.Message, statusCode: StatusCodes.Status404NotFound); }
            catch (FulfillmentConflictException ex) { return Problem(ex.Message, statusCode: StatusCodes.Status409Conflict); }
        }
    }
}
