using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using BusinessAccessLayer.Services;
using BusinessAccessLayer.DTOs.Kitchen;
using SapaFoRestRMSAPI.Hubs;

namespace SapaFoRestRMSAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class KitchenDisplayController : ControllerBase
    {
        private readonly IKitchenDisplayService _kitchenService;
        private readonly IHubContext<KitchenHub> _hubContext;

        public KitchenDisplayController(
            IKitchenDisplayService kitchenService,
            IHubContext<KitchenHub> hubContext)
        {
            _kitchenService = kitchenService;
            _hubContext = hubContext;
        }

        /// <summary>
        /// GET: api/KitchenDisplay/active-orders
        /// Get all active orders for Sous Chef screen
        /// </summary>
        [HttpGet("active-orders")]
        public async Task<IActionResult> GetActiveOrders()
        {
            try
            {
                var orders = await _kitchenService.GetActiveOrdersAsync();
                return Ok(new { success = true, data = orders });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// GET: api/KitchenDisplay/orders-by-station?courseType=MainCourse
        /// Get orders filtered by course type for station screens
        /// </summary>
        [HttpGet("orders-by-station")]
        public async Task<IActionResult> GetOrdersByStation([FromQuery] string courseType)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(courseType))
                {
                    return BadRequest(new { success = false, message = "Course type is required" });
                }

                var orders = await _kitchenService.GetOrdersByCourseTypeAsync(courseType);
                return Ok(new { success = true, data = orders });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// POST: api/KitchenDisplay/update-item-status
        /// Update status of a single menu item (from station screen)
        /// </summary>
        [HttpPost("update-item-status")]
        public async Task<IActionResult> UpdateItemStatus([FromBody] UpdateItemStatusRequest request)
        {
            try
            {
                var response = await _kitchenService.UpdateItemStatusAsync(request);

                if (!response.Success)
                {
                    return BadRequest(response);
                }

                // Broadcast real-time update via SignalR
                await _hubContext.Clients.All.SendAsync("ItemStatusChanged", new KitchenStatusChangeNotification
                {
                    OrderId = 0, // Will be filled from updated item
                    OrderDetailId = request.OrderDetailId,
                    NewStatus = request.NewStatus,
                    Timestamp = DateTime.Now,
                    ChangedBy = $"User {request.UserId}"
                });

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// POST: api/KitchenDisplay/complete-order
        /// Mark entire order as completed (from Sous Chef screen)
        /// </summary>
        [HttpPost("complete-order")]
        public async Task<IActionResult> CompleteOrder([FromBody] CompleteOrderRequest request)
        {
            try
            {
                var response = await _kitchenService.CompleteOrderAsync(request);

                if (!response.Success)
                {
                    return BadRequest(response);
                }

                // Broadcast completion via SignalR
                await _hubContext.Clients.All.SendAsync("OrderCompleted", request.OrderId);

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// GET: api/KitchenDisplay/course-types
        /// Get all available course types
        /// </summary>
        [HttpGet("course-types")]
        public async Task<IActionResult> GetCourseTypes()
        {
            try
            {
                var types = await _kitchenService.GetCourseTypesAsync();
                return Ok(new { success = true, data = types });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// GET: api/KitchenDisplay/grouped-by-item
        /// Get items grouped by menu item (theo từng món)
        /// </summary>
        [HttpGet("grouped-by-item")]
        public async Task<IActionResult> GetGroupedItemsByMenuItem()
        {
            try
            {
                var groupedItems = await _kitchenService.GetGroupedItemsByMenuItemAsync();
                return Ok(new { success = true, data = groupedItems });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// GET: api/KitchenDisplay/station-items?categoryName=Xào
        /// Get station items by category name (có 2 luồng: tất cả và urgent)
        /// </summary>
        [HttpGet("station-items")]
        public async Task<IActionResult> GetStationItems([FromQuery] string categoryName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(categoryName))
                {
                    return BadRequest(new { success = false, message = "Category name is required" });
                }

                var response = await _kitchenService.GetStationItemsByCategoryAsync(categoryName);
                return Ok(new { success = true, data = response });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// POST: api/KitchenDisplay/mark-as-urgent
        /// Mark order detail as urgent/not urgent (yêu cầu từ bếp phó)
        /// </summary>
        [HttpPost("mark-as-urgent")]
        public async Task<IActionResult> MarkAsUrgent([FromBody] MarkAsUrgentRequest request)
        {
            try
            {
                var response = await _kitchenService.MarkAsUrgentAsync(request);

                if (!response.Success)
                {
                    return BadRequest(response);
                }

                // Broadcast update via SignalR
                await _hubContext.Clients.All.SendAsync("ItemUrgentStatusChanged", new
                {
                    OrderDetailId = request.OrderDetailId,
                    IsUrgent = request.IsUrgent,
                    Timestamp = DateTime.Now
                });

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// GET: api/KitchenDisplay/station-categories
        /// Get all menu categories for stations
        /// </summary>
        [HttpGet("station-categories")]
        public async Task<IActionResult> GetStationCategories()
        {
            try
            {
                var categories = await _kitchenService.GetStationCategoriesAsync();
                return Ok(new { success = true, data = categories });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// GET: api/KitchenDisplay/recently-fulfilled-orders?minutesAgo=10
        /// Lấy danh sách các order đã hoàn thành gần đây (trong X phút)
        /// </summary>
        [HttpGet("recently-fulfilled-orders")]
        public async Task<IActionResult> GetRecentlyFulfilledOrders([FromQuery] int minutesAgo = 10)
        {
            try
            {
                var orders = await _kitchenService.GetRecentlyFulfilledOrdersAsync(minutesAgo);
                return Ok(new { success = true, data = orders });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// POST: api/KitchenDisplay/recall-order-detail
        /// Khôi phục (Recall) một order detail đã Done, đưa nó quay lại trạng thái Processing
        /// </summary>
        [HttpPost("recall-order-detail")]
        public async Task<IActionResult> RecallOrderDetail([FromBody] RecallOrderDetailRequest request)
        {
            try
            {
                var response = await _kitchenService.RecallOrderDetailAsync(request);

                if (!response.Success)
                {
                    return BadRequest(response);
                }

                // Broadcast real-time update via SignalR
                await _hubContext.Clients.All.SendAsync("ItemStatusChanged", new KitchenStatusChangeNotification
                {
                    OrderId = 0,
                    OrderDetailId = request.OrderDetailId,
                    NewStatus = "Pending",
                    Timestamp = DateTime.Now,
                    ChangedBy = $"User {request.UserId}"
                });

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
    }
}