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
    }
}
