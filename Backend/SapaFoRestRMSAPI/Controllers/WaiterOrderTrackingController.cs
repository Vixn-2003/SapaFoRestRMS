using BusinessAccessLayer.DTOs.Waiter;
using BusinessAccessLayer.Services.Interfaces;
using Microsoft.AspNetCore.SignalR;
using SapaFoRestRMSAPI.Hubs;
using BusinessAccessLayer.DTOs.Kitchen;
using Microsoft.AspNetCore.Mvc;

namespace SapaFoRestRMSAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WaiterOrderTrackingController : ControllerBase
    {
        private readonly IWaiterOrderTrackingService _service;
        private readonly IHubContext<KitchenHub> _kitchenHubContext;

        public WaiterOrderTrackingController(IWaiterOrderTrackingService service, IHubContext<KitchenHub> kitchenHubContext)
        {
            _service = service;
            _kitchenHubContext = kitchenHubContext;
        }

        /// <summary>
        /// GET: api/WaiterOrderTracking?tableIds=1,2,3
        /// Lấy danh sách orders để theo dõi tiến độ phục vụ
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetOrderTracking([FromQuery] int? waiterUserId = null, [FromQuery] string? tableIds = null)
        {
            try
            {
                List<int>? tableIdList = null;
                if (!string.IsNullOrWhiteSpace(tableIds))
                {
                    tableIdList = tableIds.Split(',')
                        .Select(id => int.TryParse(id.Trim(), out var tableId) ? tableId : (int?)null)
                        .Where(id => id.HasValue)
                        .Select(id => id!.Value)
                        .ToList();
                }
                
                var result = await _service.GetOrderTrackingAsync(waiterUserId, tableIdList);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// POST: api/WaiterOrderTracking/request-urgent
        /// Yêu cầu làm gấp một món
        /// </summary>
        [HttpPost("request-urgent")]
        public async Task<IActionResult> RequestUrgent([FromBody] RequestUrgentDto request)
        {
            try
            {
                var result = await _service.RequestUrgentAsync(request);
                if (result.Success)
                {
                    return Ok(result);
                }
                return BadRequest(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// POST: api/WaiterOrderTracking/cancel
        /// Hủy món (chưa nấu)
        /// </summary>
        [HttpPost("cancel")]
        public async Task<IActionResult> CancelOrderDetail([FromBody] CancelOrderDetailDto request)
        {
            try
            {
                var result = await _service.CancelOrderDetailAsync(request);
                if (result.Success)
                {
                    return Ok(result);
                }
                return BadRequest(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// POST: api/WaiterOrderTracking/mark-as-served
        /// Đánh dấu món đã phục vụ (lấy món)
        /// </summary>
        [HttpPost("mark-as-served")]
        public async Task<IActionResult> MarkAsServed([FromBody] MarkAsServedDto request)
        {
            try
            {
                var result = await _service.MarkAsServedAsync(request);
                if (result.Success)
                {
                    // Broadcast real-time update qua SignalR để KitchenDisplay & Waiter khác cập nhật
                    try
                    {
                        await _kitchenHubContext.Clients.All.SendAsync("ItemStatusChanged", new KitchenStatusChangeNotification
                        {
                            OrderId = 0,
                            OrderDetailId = request.OrderDetailId,
                            NewStatus = "Done",
                            Timestamp = DateTime.Now,
                            ChangedBy = "Waiter"
                        });
                    }
                    catch (Exception ex)
                    {
                        // Không chặn response nếu SignalR lỗi
                    }

                    return Ok(result);
                   
                }
                return BadRequest(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// POST: api/WaiterOrderTracking/update-quantity
        /// Cập nhật số lượng cho món có BillingType = 1 (ConsumptionBased)
        /// Cho phép tăng/giảm số lượng kể cả sau khi xác nhận
        /// </summary>
        [HttpPost("update-quantity")]
        public async Task<IActionResult> UpdateQuantity([FromBody] UpdateQuantityDto request)
        {
            try
            {
                var result = await _service.UpdateQuantityAsync(request);
                if (result.Success)
                {
                    return Ok(result);
                }
                return BadRequest(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// POST: api/WaiterOrderTracking/confirm-consumption-quantity
        /// Xác nhận số lượng đã lấy cho món có BillingType = 1 (ConsumptionBased)
        /// Không cần chờ bếp, phục vụ có thể tự chủ động xác nhận
        /// </summary>
        [HttpPost("confirm-consumption-quantity")]
        public async Task<IActionResult> ConfirmConsumptionQuantity([FromBody] ConfirmConsumptionQuantityDto request)
        {
            try
            {
                var result = await _service.ConfirmConsumptionQuantityAsync(request);
                if (result.Success)
                {
                    return Ok(result);
                }
                return BadRequest(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

    }
}

