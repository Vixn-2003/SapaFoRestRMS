using BusinessAccessLayer.DTOs.OrderConfirmation;
using BusinessAccessLayer.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SapaFoRestRMSAPI.Controllers
{
    /// <summary>
    /// API Controller cho xác nhận hóa đơn
    /// Xử lý 2 loại món: Kitchen-prepared và Consumption-based
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OrderConfirmationController : ControllerBase
    {
        private readonly IOrderConfirmationService _confirmationService;
        private readonly ILogger<OrderConfirmationController> _logger;
        
        public OrderConfirmationController(
            IOrderConfirmationService confirmationService,
            ILogger<OrderConfirmationController> logger)
        {
            _confirmationService = confirmationService;
            _logger = logger;
        }
        
        /// <summary>
        /// Lấy thông tin đơn hàng để xác nhận
        /// GET: api/orderconfirmation/{orderId}
        /// </summary>
        [HttpGet("{orderId}")]
        [ProducesResponseType(typeof(OrderConfirmationDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<OrderConfirmationDto>> GetOrderForConfirmation(int orderId)
        {
            try
            {
                var result = await _confirmationService.GetOrderForConfirmationAsync(orderId);
                
                if (result == null)
                {
                    return NotFound(new { message = $"Không tìm thấy đơn hàng với ID: {orderId}" });
                }
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting order confirmation for OrderId: {OrderId}", orderId);
                return StatusCode(500, new { message = "Có lỗi xảy ra khi tải thông tin đơn hàng" });
            }
        }
        
        /// <summary>
        /// Xác nhận hóa đơn
        /// POST: api/orderconfirmation/confirm
        /// </summary>
        [HttpPost("confirm")]
        [ProducesResponseType(typeof(OrderConfirmationDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<OrderConfirmationDto>> ConfirmOrder([FromBody] ConfirmOrderRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }
                
                var result = await _confirmationService.ConfirmOrderAsync(request);
                
                _logger.LogInformation("Order {OrderId} confirmed successfully", request.OrderId);
                
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Order not found: {OrderId}", request.OrderId);
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation when confirming order: {OrderId}", request.OrderId);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming order: {OrderId}", request.OrderId);
                return StatusCode(500, new { message = "Có lỗi xảy ra khi xác nhận đơn hàng" });
            }
        }
        
        /// <summary>
        /// Hủy món (chỉ cho Kitchen items ở trạng thái NotStarted)
        /// POST: api/orderconfirmation/cancel-item
        /// </summary>
        [HttpPost("cancel-item")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CancelItem([FromBody] CancelItemRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }
                
                var result = await _confirmationService.CancelItemAsync(request);
                
                _logger.LogInformation("Item {OrderDetailId} cancelled successfully", request.OrderDetailId);
                
                return Ok(new { success = true, message = "Hủy món thành công" });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Item not found: {OrderDetailId}", request.OrderDetailId);
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Cannot cancel item: {OrderDetailId}", request.OrderDetailId);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling item: {OrderDetailId}", request.OrderDetailId);
                return StatusCode(500, new { message = "Có lỗi xảy ra khi hủy món" });
            }
        }
        
        /// <summary>
        /// Kiểm tra xem món có thể hủy không
        /// GET: api/orderconfirmation/can-cancel/{orderDetailId}
        /// </summary>
        [HttpGet("can-cancel/{orderDetailId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> CanCancelItem(int orderDetailId)
        {
            try
            {
                var (canCancel, reason) = await _confirmationService.ValidateCanCancelItemAsync(orderDetailId);
                
                return Ok(new { canCancel, reason });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if item can be cancelled: {OrderDetailId}", orderDetailId);
                return StatusCode(500, new { message = "Có lỗi xảy ra" });
            }
        }
    }
}

