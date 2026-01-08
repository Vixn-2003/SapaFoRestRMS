using BusinessAccessLayer.DTOs.CounterStaff;
using BusinessAccessLayer.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SapaFoRestRMSAPI.Controllers
{
    /// <summary>
    /// API Controller cho Counter Staff Order Management - UC123
    /// Counter Staff: View list order
    /// </summary>
    [ApiController]
    [Route("api/counter/orders")]

    public class CounterStaffOrderController : ControllerBase
    {
        private readonly ICounterStaffOrderService _orderService;

        public CounterStaffOrderController(ICounterStaffOrderService orderService)
        {
            _orderService = orderService;
        }

        /// <summary>
        /// GET: /api/counter/orders
        /// Lấy danh sách orders theo filter
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetOrders(
            [FromQuery] string? status = null,
            [FromQuery] DateOnly? date = null,
            [FromQuery] string? tableNumber = null,
            [FromQuery] string? searchKeyword = null,
            CancellationToken ct = default)
        {
            try
            {
                var filter = new OrderListFilterDto
                {
                    Status = status,
                    Date = date,
                    TableNumber = tableNumber,
                    SearchKeyword = searchKeyword
                };

                var orders = await _orderService.GetAllOrdersAsync(filter, ct);
                return Ok(orders);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy danh sách orders", error = ex.Message });
            }
        }

        /// <summary>
        /// GET: /api/counter/orders/{id}
        /// Lấy order summary theo ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrderSummary(int id, CancellationToken ct = default)
        {
            try
            {
                var order = await _orderService.GetOrderSummaryAsync(id, ct);
                if (order == null)
                {
                    return NotFound(new { message = $"Không tìm thấy order với ID: {id}" });
                }

                return Ok(order);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy order summary", error = ex.Message });
            }
        }
    }
}

