using BusinessAccessLayer.DTOs.OrderGuest;
using BusinessAccessLayer.Services;
using BusinessAccessLayer.Services.Interfaces;
using DataAccessLayer.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using SapaFoRestRMSAPI.Hubs;
using static BusinessAccessLayer.Services.Interfaces.IDashboardTableService;

namespace SapaFoRestRMSAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DashboardTableController : ControllerBase
    {
        private readonly IDashboardTableService _dashboardTableService;
        private readonly IKitchenDisplayService _kitchenDisplayService;
        private readonly IHubContext<KitchenHub> _kitchenHubContext;

        public DashboardTableController(
            IDashboardTableService dashboardTableService,
            IKitchenDisplayService kitchenDisplayService,
            IHubContext<KitchenHub> kitchenHubContext)
        {
            _dashboardTableService = dashboardTableService;
            _kitchenDisplayService = kitchenDisplayService;
            _kitchenHubContext = kitchenHubContext;
        }


        [HttpGet("List-Table")]
        public async Task<IActionResult> GetDashboardTableData([FromQuery] string? areaName,
        [FromQuery] int? floor,
        [FromQuery] string? status,
        [FromQuery] string? searchString,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12)
        {
            try
            {
                var data = await _dashboardTableService.GetDashboardDataAsync(areaName, floor, status, searchString, page, pageSize);
                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi máy chủ nội bộ: {ex.Message}" });
            }
        }

        // (1) API List (Không đổi)
        [HttpGet]
        public async Task<IActionResult> GetReservations([FromQuery] ReservationQueryParameters parameters)
        {
            var pagedResult = await _dashboardTableService.GetReservationsAsync(parameters);

            Response.Headers.Add("X-Pagination", System.Text.Json.JsonSerializer.Serialize(new
            {
                pagedResult.TotalCount,
                pagedResult.PageSize,
                pagedResult.PageNumber,
                pagedResult.HasNextPage,
                pagedResult.HasPreviousPage
            }));

            return Ok(pagedResult.Items);
        }

        // (2) API Detail (Thay đổi: Guid -> int)
        [HttpGet("{id:int}")] // Thêm ràng buộc :int
        public async Task<IActionResult> GetReservationDetail(int id)
        {
            try
            {
                var result = await _dashboardTableService.GetReservationDetailAsync(id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        // (3) API Đổi trạng thái 
        [HttpPut("{id:int}/seat")]
        public async Task<IActionResult> SeatGuest(int id)
        {
            try
            {
                // Gọi service và nhận lại kết quả
                var result = await _dashboardTableService.SeatGuestAsync(id);

                // Lấy ra TableId (Vì 1 đơn có thể gộp nhiều bàn, ta lấy bàn đầu tiên hoặc xử lý logic tùy ý)
                var mainTableId = result.ReservationTables?.FirstOrDefault()?.TableId;

                return Ok(new
                {
                    success = true,
                    message = "Xác nhận khách ngồi thành công!",
                    // Trả về dữ liệu cho Ajax cập nhật UI
                    tableId = mainTableId,
                    guestSeatedTime = result.ArrivalAt
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("MenuOrder/{tableId}")]
        public async Task<IActionResult> GetMenuOrder(
         int tableId,
         [FromQuery] int? categoryId,
         [FromQuery] string? searchString)
        {
            try
            {
                var result = await _dashboardTableService.GetStaffOrderScreenAsync(tableId, categoryId, searchString);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
        {
            try
            {
                var categories = await _dashboardTableService.GetAllCategoriesAsync();
                return Ok(categories);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        // Xem các món đã gọi
        [HttpPost("SaveChanges")]
        public async Task<IActionResult> SaveChanges([FromBody] SaveOrderRequest request)
        {
            if (request == null || request.Items == null || !request.Items.Any())
                return BadRequest(new { message = "Dữ liệu không hợp lệ" });

            try
            {
                await _dashboardTableService.SaveOrderChangesAsync(request);

                //  Broadcast đơn mới đến màn hình bếp nếu có món mới được thêm
                var hasNewItems = request.Items.Any(item => item.Action == "Add");
                if (hasNewItems)
                {
                    try
                    {
                        // Lấy order mới từ KitchenDisplayService
                        var activeOrders = await _kitchenDisplayService.GetActiveOrdersAsync();
                        
                        // Lấy order mới nhất (theo CreatedAt) - đơn vừa được thêm sẽ là mới nhất
                        // Hoặc có thể lấy tất cả orders mới trong vài giây gần đây
                        var recentOrders = activeOrders
                            .Where(o => o.CreatedAt >= DateTime.Now.AddMinutes(-1)) // Orders trong 1 phút gần đây
                            .OrderByDescending(o => o.CreatedAt)
                            .ToList();

                        // Broadcast tất cả orders mới
                        foreach (var newOrder in recentOrders)
                        {
                            await _kitchenHubContext.Clients.All.SendAsync("NewOrderReceived", newOrder);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log error nhưng không fail request
                        Console.WriteLine($"Warning: Không thể broadcast đơn mới đến bếp: {ex.Message}");
                    }
                }

                return Ok(new { success = true, message = "Lưu thành công!" });
            }
            catch (Exception ex)
            {
                // In lỗi chi tiết ra nếu có InnerException (quan trọng để debug lỗi SQL)
                var msg = ex.InnerException?.Message ?? ex.Message;
                return BadRequest(new { success = false, message = msg });
            }
        }


    }
}
