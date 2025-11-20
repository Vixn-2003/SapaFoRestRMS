using BusinessAccessLayer.DTOs.Inventory;
using BusinessAccessLayer.DTOs.Manager;
using BusinessAccessLayer.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace SapaFoRestRMSAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UnitController : Controller
    {
        private readonly IUnitService _unitService;

        public UnitController(IUnitService unitService)
        {
            _unitService = unitService;
        }


        [HttpGet]
        public async Task<ActionResult<IEnumerable<UnitDTO>>> GetAllUnit()
        {
            try
            {
                // Get list category
                var unit = await _unitService.GetAllUnits();
                if (!unit.Any())
                {
                    //Can't find category
                    return NotFound("No unit found");
                }
                // Find list category
                return Ok(unit);
            }
            //Error
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}
