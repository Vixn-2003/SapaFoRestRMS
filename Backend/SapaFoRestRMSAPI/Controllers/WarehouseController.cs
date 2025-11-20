using BusinessAccessLayer.DTOs.Inventory;
using BusinessAccessLayer.DTOs.Manager;
using BusinessAccessLayer.Services;
using BusinessAccessLayer.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace SapaFoRestRMSAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WarehouseController : ControllerBase
    {
        private readonly IWarehouseService _warehouseService;

        public WarehouseController(IWarehouseService warehouseService)
        {
            _warehouseService = warehouseService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<WarehouseDTO>>> GetAllWarehouse()
        {
            try
            {
                var warehouse = await _warehouseService.GetAllWarehouse();
                if (!warehouse.Any())
                {
                    return NotFound("No warehouse found");
                }
                return Ok(warehouse);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while getting the warehouse");
            }
        }
    }
}
