using BusinessAccessLayer.Services.Interfaces;
using DomainAccessLayer.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace SapaFoRestRMSAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderTableController : ControllerBase
    {
        private readonly IOrderTableService _orderTableService;

        public OrderTableController(IOrderTableService orderTableService)
        {
            _orderTableService = orderTableService;
        }

        /// <summary>
        /// Lấy danh sách bàn mà khách đã đến (lọc theo ngày & khung giờ)
        /// </summary>
        [HttpGet("tables-by-status")]
        public async Task<ActionResult<IEnumerable<Table>>> GetTablesByReservationStatus([FromQuery] string status)
        {
            if (string.IsNullOrEmpty(status))
                return BadRequest("Status parameter is required.");

            var tables = await _orderTableService.GetTablesByReservationStatusAsync(status);

            if (!tables.Any())
                return NotFound("No tables found for this status.");

            return Ok(tables);
        }


        [HttpGet("by-reservation-status")]
        public async Task<IActionResult> GetTablesByReservationStatus(
      [FromQuery] string status,
      [FromQuery] int page = 1,
      [FromQuery] int pageSize = 18)
        {
            if (string.IsNullOrWhiteSpace(status))
                return BadRequest("Status is required.");

            if (page <= 0 || pageSize <= 0)
                return BadRequest("Page and pageSize must be greater than 0.");

            var result = await _orderTableService.GetTablesByReservationStatusAsync(status, page, pageSize);

            return Ok(result);
        }

        [HttpGet("tables-by-reservation")]
        public async Task<IActionResult> GetTablesByReservation(int reservationId, string status)
        {
            var result = await _orderTableService.GetTablesByReservationIdAndStatusAsync(reservationId, status);
            if (!result.Any())
                return NotFound("No tables found for this reservation and status.");

            return Ok(result);
        }

        [HttpGet("reservation/{reservationId}")]
        public async Task<IActionResult> GetMenuForReservation(int reservationId, [FromQuery] string status)
        {
            try
            {
                var menu = await _orderTableService.GetMenuForReservationAsync(reservationId, status);
                if (!menu.Any())
                    return NotFound("No available menu items found.");

                return Ok(menu);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

    }
}
