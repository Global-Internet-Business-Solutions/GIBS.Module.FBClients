using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using GIBS.Module.FBClients.Inventory.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Oqtane.Controllers;
using Oqtane.Enums;
using Oqtane.Infrastructure;
using Oqtane.Shared;

namespace GIBS.Module.FBClients.Inventory.Controllers
{
    [Route(ControllerRoutes.ApiRoute)]
    [Authorize]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public class FBClientsInventoryController(IInventoryService inventory, ILogManager logger, IHttpContextAccessor accessor)
        : ModuleControllerBase(logger, accessor)
    {
        [HttpGet("{moduleId:int}/catalog"), Authorize(Policy = PolicyNames.ViewModule)]
        public Task<IActionResult> Catalog(int moduleId) => Execute(moduleId, async () => Ok(await inventory.GetCatalogAsync(moduleId)));

        [HttpGet("{moduleId:int}/invoices"), Authorize(Policy = PolicyNames.ViewModule)]
        public Task<IActionResult> Invoices(int moduleId, [FromQuery] int page, [FromQuery] string search) =>
            Execute(moduleId, async () => Ok(await inventory.GetInvoicesAsync(moduleId, page, search)));

        [HttpGet("{moduleId:int}/invoices/{invoiceId:int}"), Authorize(Policy = PolicyNames.ViewModule)]
        public Task<IActionResult> Invoice(int moduleId, int invoiceId) =>
            Execute(moduleId, async () => Ok(await inventory.GetInvoiceAsync(moduleId, invoiceId)));

        [HttpGet("{moduleId:int}/report"), Authorize(Policy = PolicyNames.ViewModule)]
        public Task<IActionResult> Report(int moduleId, [FromQuery, Required] DateOnly? start, [FromQuery, Required] DateOnly? end) =>
            Execute(moduleId, async () => Ok(await inventory.GetReportAsync(moduleId, start!.Value, end!.Value)));

        [HttpPost("{moduleId:int}/categories"), Authorize(Policy = PolicyNames.EditModule)]
        public Task<IActionResult> Category(int moduleId, [FromBody] InventoryCategory input) =>
            Execute(moduleId, async () => Ok(await inventory.SaveCategoryAsync(moduleId, input)));

        [HttpPost("{moduleId:int}/products"), Authorize(Policy = PolicyNames.EditModule)]
        public Task<IActionResult> Product(int moduleId, [FromBody] InventoryProduct input) =>
            Execute(moduleId, async () => Ok(await inventory.SaveProductAsync(moduleId, input)));

        [HttpPost("{moduleId:int}/suppliers"), Authorize(Policy = PolicyNames.EditModule)]
        public Task<IActionResult> Supplier(int moduleId, [FromBody] InventorySupplier input) =>
            Execute(moduleId, async () => Ok(await inventory.SaveSupplierAsync(moduleId, input)));

        [HttpPost("{moduleId:int}/invoices"), Authorize(Policy = PolicyNames.EditModule)]
        public Task<IActionResult> SaveInvoice(int moduleId, [FromBody] InventoryInvoice input) =>
            Execute(moduleId, async () => Ok(await inventory.SaveInvoiceAsync(moduleId, input)));

        [HttpPost("{moduleId:int}/invoices/{invoiceId:int}/lines"), Authorize(Policy = PolicyNames.EditModule)]
        public Task<IActionResult> Line(int moduleId, int invoiceId, [FromBody] InventoryLine input) =>
            Execute(moduleId, async () => Ok(await inventory.SaveLineAsync(moduleId, invoiceId, input)));

        [HttpDelete("{moduleId:int}/invoices/{invoiceId:int}"), Authorize(Policy = PolicyNames.EditModule)]
        public Task<IActionResult> DeleteInvoice(int moduleId, int invoiceId) => Execute(moduleId, async () =>
        {
            await inventory.DeleteInvoiceAsync(moduleId, invoiceId);
            return NoContent();
        });

        [HttpDelete("{moduleId:int}/invoices/{invoiceId:int}/lines/{lineId:int}"), Authorize(Policy = PolicyNames.EditModule)]
        public Task<IActionResult> DeleteLine(int moduleId, int invoiceId, int lineId) => Execute(moduleId, async () =>
        {
            await inventory.DeleteLineAsync(moduleId, invoiceId, lineId);
            return NoContent();
        });

        private async Task<IActionResult> Execute(int moduleId, Func<Task<IActionResult>> action)
        {
            if (!IsAuthorizedEntityId(EntityNames.Module, moduleId)) return Forbid();
            if (!ModelState.IsValid) return ValidationProblem(ModelState);
            try
            {
                return await action();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (ValidationException ex)
            {
                return Problem(ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
            catch (KeyNotFoundException ex)
            {
                return Problem(ex.Message, statusCode: StatusCodes.Status404NotFound);
            }
            catch (InventorySaveException ex)
            {
                _logger.Log(LogLevel.Warning, this, LogFunction.Update, "Inventory update failed in module {ModuleId}: {ErrorType}", moduleId, ex.InnerException.GetType().Name);
                return Problem(ex.Message, statusCode: StatusCodes.Status409Conflict);
            }
        }
    }
}
