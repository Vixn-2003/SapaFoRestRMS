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
        /// GET: api/KitchenDisplay/active-orders?statusFilter=Pending
        /// Get all active orders for Sous Chef screen
        /// </summary>
        /// <param name="statusFilter">Optional: Filter by item status (Pending, Cooking, Late, Ready). Null or empty = all</param>
        [HttpGet("active-orders")]
        public async Task<IActionResult> GetActiveOrders([FromQuery] string? statusFilter = null)
        {
            try
            {
                var orders = await _kitchenService.GetActiveOrdersAsync(statusFilter);
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
        /// POST: api/KitchenDisplay/start-cooking-with-quantity
        /// Start cooking with specific quantity (split order detail if quantity < total)
        /// </summary>
        [HttpPost("start-cooking-with-quantity")]
        public async Task<IActionResult> StartCookingWithQuantity([FromBody] StartCookingWithQuantityRequest request)
        {
            try
            {
                var response = await _kitchenService.StartCookingWithQuantityAsync(request);

                if (!response.Success)
                {
                    return BadRequest(response);
                }

                // Broadcast real-time update via SignalR
                await _hubContext.Clients.All.SendAsync("ItemStatusChanged", new KitchenStatusChangeNotification
                {
                    OrderId = 0,
                    OrderDetailId = response.UpdatedItem?.OrderDetailId ?? request.OrderDetailId,
                    NewStatus = "Cooking",
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
        /// GET: api/KitchenDisplay/grouped-by-item?statusFilter=Pending
        /// Get items grouped by menu item (theo từng món)
        /// </summary>
        /// <param name="statusFilter">Optional: Filter by item status (Pending, Cooking, Late, Ready). Null or empty = all</param>
        [HttpGet("grouped-by-item")]
        public async Task<IActionResult> GetGroupedItemsByMenuItem([FromQuery] string? statusFilter = null)
        {
            try
            {
                var groupedItems = await _kitchenService.GetGroupedItemsByMenuItemAsync(statusFilter);
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
        /// GET: api/KitchenDisplay/order-details/{orderId}
        /// Get order details with all items including Done status (for modal display)
        /// </summary>
        [HttpGet("order-details/{orderId}")]
        public async Task<IActionResult> GetOrderDetailsWithAllItems(int orderId)
        {
            try
            {
                var order = await _kitchenService.GetOrderDetailsWithAllItemsAsync(orderId);
                if (order == null)
                {
                    return NotFound(new { success = false, message = "Order not found" });
                }
                return Ok(new { success = true, data = order });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// POST: api/KitchenDisplay/recall-order-detail
        /// Khôi phục (Recall) một order detail đã Done, đưa nó quay lại trạng thái Pending
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

        /// <summary>
        /// POST: api/KitchenDisplay/print-item-ticket
        /// In ticket cho món đã hoàn thành
        /// </summary>
        [HttpPost("print-item-ticket")]
        public async Task<IActionResult> PrintItemTicket([FromBody] PrintItemTicketRequest request)
        {
            try
            {
                // Lấy thông tin order detail và order
                var orderDetail = await _kitchenService.GetOrderDetailForPrintAsync(request.OrderDetailId, request.OrderComboItemId);
                
                if (orderDetail == null)
                {
                    return NotFound(new { success = false, message = "Không tìm thấy món ăn" });
                }

                // Trả về thông tin để frontend in
                return Ok(new 
                { 
                    success = true, 
                    data = orderDetail 
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// POST: api/KitchenDisplay/batch-cook
        /// Gom nhiều hành động bắt đầu nấu vào một call để giảm số lượng fetch từ frontend
        /// </summary>
        [HttpPost("batch-cook")]
        public async Task<IActionResult> BatchCook([FromBody] BatchCookRequest request)
        {
            try
            {
                if (request.Items == null || !request.Items.Any())
                {
                    return BadRequest(new { success = false, message = "Danh sách món trống" });
                }

                var result = await _kitchenService.BatchStartCookingAsync(request);

                if (!result.Success)
                {
                    return Ok(new { success = false, message = result.Message, items = result.Items });
                }

                return Ok(new { success = true, message = result.Message, items = result.Items });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
    }
}