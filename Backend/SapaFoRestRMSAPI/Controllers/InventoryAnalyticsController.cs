using BusinessAccessLayer.DTOs.Inventory;
using BusinessAccessLayer.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace SapaFoRestRMSAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InventoryAnalyticsController : ControllerBase
    {
        private readonly IInventoryAnalyticsService _service;

        public InventoryAnalyticsController(IInventoryAnalyticsService service)
        {
            _service = service;
        }

        [HttpGet("usage")]
        public async Task<IActionResult> GetUsage([FromQuery] int daysWindow = 30)
        {
            var data = await _service.GetIngredientUsageForecastAsync(daysWindow);
            return Ok(data);
        }

        [HttpPost("reorder-levels")]
        public async Task<IActionResult> UpdateReorder([FromQuery] int daysWindow = 30)
        {
            var updated = await _service.RecalculateReorderLevelsAsync(daysWindow);
            return Ok(new
            {
                Success = true,
                UpdatedCount = updated
            });
        }
    }

}
