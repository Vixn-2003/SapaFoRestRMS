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

        [HttpGet("warehouse/{warehouseId}")]
        public async Task<IActionResult> GetBatchesByWarehouse(int warehouseId)
        {
            var batches = await _warehouseService.GetBatchesByWarehouseAsync(warehouseId);
            return Ok(batches);
        }


        [HttpPut("{id}")]
        public async Task<ActionResult<bool>> UpdateWarehouse(int id, [FromBody] WarehouseDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest("Warehouse name is required.");

            try
            {
                var warehouse = await _warehouseService.GetAllWarehouse();

                foreach( var ware in warehouse)
                {
                    if (ware.Name.Equals(dto.Name))
                    {
                        return false;
                    }
                }

                await _warehouseService.UpdateWarehouseAsync(id, dto);
                return true; // 204 - Cập nhật thành công
            }
            catch (InvalidOperationException ex)
            {
                // Warehouse không tồn tại
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentNullException ex)
            {
                // Entity null (không nên xảy ra)
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                // Lỗi khác
                return StatusCode(500, new { message = "An error occurred while updating the warehouse.", detail = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteWarehouse(int id)
        {
            try
            {
                await _warehouseService.DeleteWarehouseAsync(id);
                return NoContent(); // 204
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error deleting warehouse" });
            }
        }


        [HttpPost]
        public async Task<ActionResult<bool>> CreateWarehouse([FromBody] WarehouseDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var createdWarehouse = await _warehouseService.CreateWarehouseAsync(dto);

                // Trả về 201 Created với location header
                return createdWarehouse;
            }
            catch (InvalidOperationException ex)
            {
                // Warehouse name đã tồn tại
                return Conflict(new { message = ex.Message });
            }
            catch (ArgumentNullException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while creating the warehouse.", detail = ex.Message });
            }
        }
    }
}
