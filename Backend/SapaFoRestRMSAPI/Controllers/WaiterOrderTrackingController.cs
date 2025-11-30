using Microsoft.AspNetCore.Mvc;
using BusinessAccessLayer.DTOs.Waiter;
using BusinessAccessLayer.Services.Interfaces;

namespace SapaFoRestRMSAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WaiterOrderTrackingController : ControllerBase
    {
        private readonly IWaiterOrderTrackingService _service;

        public WaiterOrderTrackingController(IWaiterOrderTrackingService service)
        {
            _service = service;
        }

        /// <summary>
        /// GET: api/WaiterOrderTracking
        /// Lấy danh sách orders để theo dõi tiến độ phục vụ
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetOrderTracking([FromQuery] int? waiterUserId = null)
        {
            try
            {
                var result = await _service.GetOrderTrackingAsync(waiterUserId);
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

