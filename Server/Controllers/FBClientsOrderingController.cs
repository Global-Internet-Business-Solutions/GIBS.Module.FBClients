using System.Threading.Tasks;
using GIBS.Module.FBClients.Ordering.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oqtane.Shared;

namespace GIBS.Module.FBClients.Ordering.Controllers
{
    [Route(ControllerRoutes.ApiRoute)]
    [AllowAnonymous]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public class FBClientsOrderingController(IFoodOrderService orders) : ControllerBase
    {
        [HttpPost("{moduleId:int}/open")]
        [RequestSizeLimit(4096)]
        public async Task<IActionResult> Open(int moduleId, [FromBody] FoodOrderRequest request)
        {
            if (!ModelState.IsValid || request == null) return BadRequest();
            var sheet = await orders.OpenAsync(moduleId, request.Key);
            return sheet == null ? NotFound() : Ok(sheet);
        }

        [HttpPost("{moduleId:int}/submit")]
        [RequestSizeLimit(16384)]
        public async Task<IActionResult> Submit(int moduleId, [FromBody] FoodOrderRequest request)
        {
            if (!ModelState.IsValid || request == null) return BadRequest();
            return Ok(await orders.SubmitAsync(moduleId, request.Key, request.Quantities));
        }
    }
}
