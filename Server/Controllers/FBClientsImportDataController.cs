using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Oqtane.Controllers;
using Oqtane.Infrastructure;
using Oqtane.Shared;

namespace GIBS.Module.FBClients.ImportData
{
    [Route(ControllerRoutes.ApiRoute)]
    [Authorize]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public class FBClientsImportDataController(IImportDataService service, ILogManager logger, IHttpContextAccessor accessor)
        : ModuleControllerBase(logger, accessor)
    {
        [HttpGet("{moduleId:int}"), Authorize(Policy = PolicyNames.ViewModule)]
        public Task<IActionResult> Configuration(int moduleId) => Execute(moduleId, async () => Ok(await service.GetConfigurationAsync(moduleId)));

        [HttpGet("{moduleId:int}/connection"), Authorize(Policy = PolicyNames.EditModule)]
        public Task<IActionResult> HasConnection(int moduleId) => Execute(moduleId, async () => Ok(await service.HasConnectionAsync(moduleId)));

        [HttpPost("{moduleId:int}/connection"), Authorize(Policy = PolicyNames.EditModule)]
        public Task<IActionResult> Connection(int moduleId, [FromBody] ImportConnectionChange change) => Execute(moduleId, async () =>
        {
            await service.SaveConnectionAsync(moduleId, change);
            return NoContent();
        });

        [HttpGet("{moduleId:int}/tables"), Authorize(Policy = PolicyNames.ViewModule)]
        public Task<IActionResult> Tables(int moduleId, [FromQuery] string source) => Execute(moduleId, async () => Ok(await service.InspectAsync(moduleId, source)));

        [HttpPost("{moduleId:int}/plan"), Authorize(Policy = PolicyNames.EditModule)]
        public Task<IActionResult> Save(int moduleId, [FromBody] ImportDataPlan plan) => Execute(moduleId, async () =>
        {
            await service.SavePlanAsync(moduleId, plan);
            return NoContent();
        });

        [HttpPost("{moduleId:int}/preflight"), Authorize(Policy = PolicyNames.ViewModule)]
        public Task<IActionResult> Preflight(int moduleId, [FromBody] ImportPreflightRequest request) =>
            Execute(moduleId, async () => Ok(await service.PreflightAsync(moduleId, request)));

        [HttpPost("{moduleId:int}/reconciliation"), Authorize(Policy = PolicyNames.ViewModule)]
        public Task<IActionResult> Reconciliation(int moduleId, [FromBody] ImportPreflightRequest request) =>
            Execute(moduleId, async () => Ok(await service.ReconcileAsync(moduleId, request)));

        [HttpPost("{moduleId:int}/rehearsal"), Authorize(Policy = PolicyNames.EditModule)]
        public Task<IActionResult> Rehearsal(int moduleId, [FromBody] ImportRehearsalRequest request) =>
            Execute(moduleId, async () => Ok(await service.RunRehearsalBatchAsync(moduleId, request)));

        [HttpPost("{moduleId:int}/turkey-rehearsal"), Authorize(Policy = PolicyNames.EditModule)]
        public Task<IActionResult> TurkeyRehearsal(int moduleId, [FromBody] ImportRehearsalRequest request) =>
            Execute(moduleId, async () => Ok(await service.RunTurkeyRehearsalBatchAsync(moduleId, request)));

        [HttpGet("{moduleId:int}/inventory"), Authorize(Policy = PolicyNames.ViewModule)]
        public Task<IActionResult> InventoryConfiguration(int moduleId) => Execute(moduleId, async () => Ok(await service.GetInventoryConfigurationAsync(moduleId)));

        [HttpGet("{moduleId:int}/inventory/tables"), Authorize(Policy = PolicyNames.ViewModule)]
        public Task<IActionResult> InventoryTables(int moduleId, [FromQuery] string source) => Execute(moduleId, async () => Ok(await service.InspectInventoryAsync(moduleId, source)));

        [HttpPost("{moduleId:int}/inventory/plan"), Authorize(Policy = PolicyNames.EditModule)]
        public Task<IActionResult> InventorySave(int moduleId, [FromBody] InventoryImportPlan plan) => Execute(moduleId, async () =>
        {
            await service.SaveInventoryPlanAsync(moduleId, plan);
            return NoContent();
        });

        [HttpPost("{moduleId:int}/inventory/preflight"), Authorize(Policy = PolicyNames.ViewModule)]
        public Task<IActionResult> InventoryPreflight(int moduleId) => Execute(moduleId, async () => Ok(await service.InventoryPreflightAsync(moduleId)));

        [HttpPost("{moduleId:int}/inventory/reconciliation"), Authorize(Policy = PolicyNames.ViewModule)]
        public Task<IActionResult> InventoryReconciliation(int moduleId) => Execute(moduleId, async () => Ok(await service.InventoryReconcileAsync(moduleId)));

        [HttpPost("{moduleId:int}/inventory/rehearsal"), Authorize(Policy = PolicyNames.EditModule)]
        public Task<IActionResult> InventoryRehearsal(int moduleId, [FromBody] InventoryImportRunRequest request) => Execute(moduleId, async () => Ok(await service.RunInventoryRehearsalAsync(moduleId, request)));

        private async Task<IActionResult> Execute(int moduleId, Func<Task<IActionResult>> action)
        {
            if (!IsAuthorizedEntityId(EntityNames.Module, moduleId)) return Forbid();
            if (!ModelState.IsValid) return ValidationProblem(ModelState);
            try { return await action(); }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (ValidationException ex) { return Problem(ex.Message, statusCode: StatusCodes.Status400BadRequest); }
            catch (ImportDataException ex) { return Problem(ex.Message, statusCode: StatusCodes.Status503ServiceUnavailable); }
        }
    }
}
