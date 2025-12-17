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

        [HttpDelete]
        public async Task<ActionResult<IEnumerable<UnitDTO>>> DeleteWarehouse(int id)
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

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] UnitDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.UnitName))
                return BadRequest("Unit name is required");

            try
            {
                var unit = await _unitService.CreateAsync(dto);
                return Ok(unit);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        // ============ UPDATE UNIT ============
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UnitDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.UnitName))
                return BadRequest("Unit name is required");

            try
            {
                await _unitService.UpdateAsync(id, dto);
                return NoContent(); // 204
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }
    }
}
