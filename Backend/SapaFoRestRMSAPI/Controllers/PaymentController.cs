using BusinessAccessLayer.DTOs.Payment;
using BusinessAccessLayer.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SapaFoRestRMSAPI.Controllers;

/// <summary>
/// Controller xử lý các API thanh toán
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize] // Yêu cầu authentication
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    /// <summary>
    /// Lấy danh sách đơn hàng chờ thanh toán
    /// GET /api/payment/orders?status=pending-payment
    /// </summary>
    [HttpGet("orders")]
    public async Task<IActionResult> GetPendingOrders(
        [FromQuery] string status = "pending-payment",
        CancellationToken ct = default)
    {
        try
        {
            var orders = await _paymentService.GetPendingOrdersAsync(ct);
            return Ok(orders);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Lỗi khi lấy danh sách đơn hàng", error = ex.Message });
        }
    }

    /// <summary>
    /// Lấy chi tiết đơn hàng
    /// GET /api/payment/orders/{id}/details
    /// </summary>
    [HttpGet("orders/{id}/details")]
    public async Task<IActionResult> GetOrderDetail(int id, CancellationToken ct = default)
    {
        try
        {
            var order = await _paymentService.GetOrderDetailAsync(id, ct);
            
            if (order == null)
            {
                return NotFound(new { message = $"Không tìm thấy đơn hàng với ID: {id}" });
            }

            return Ok(order);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Lỗi khi lấy chi tiết đơn hàng", error = ex.Message });
        }
    }

    /// <summary>
    /// Áp dụng ưu đãi/giảm giá
    /// POST /api/payment/discounts/validate
    /// </summary>
    [HttpPost("discounts/validate")]
    public async Task<IActionResult> ValidateDiscount([FromBody] DiscountRequestDto request, CancellationToken ct = default)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var order = await _paymentService.ApplyDiscountAsync(request, ct);
            return Ok(new { 
                success = true, 
                message = "Áp dụng ưu đãi thành công",
                order = order 
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Lỗi khi áp dụng ưu đãi", error = ex.Message });
        }
    }

    /// <summary>
    /// Khởi tạo giao dịch thanh toán
    /// POST /api/payment/payments/initiate
    /// </summary>
    [HttpPost("payments/initiate")]
    public async Task<IActionResult> InitiatePayment([FromBody] PaymentInitiateRequestDto request, CancellationToken ct = default)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var transaction = await _paymentService.InitiatePaymentAsync(request, ct);
            return Ok(transaction);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Lỗi khi khởi tạo thanh toán", error = ex.Message });
        }
    }

    /// <summary>
    /// Xác nhận thanh toán
    /// POST /api/payment/payments/confirm
    /// </summary>
    [HttpPost("payments/confirm")]
    public async Task<IActionResult> ConfirmPayment([FromBody] PaymentRequestDto request, CancellationToken ct = default)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var transaction = await _paymentService.ProcessPaymentAsync(request, ct);
            return Ok(new { 
                success = true, 
                message = "Thanh toán thành công",
                transaction = transaction 
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Lỗi khi xử lý thanh toán", error = ex.Message });
        }
    }

    /// <summary>
    /// Lấy kết quả thanh toán theo sessionId
    /// GET /api/payment/payments/result/{sessionId}
    /// </summary>
    [HttpGet("payments/result/{sessionId}")]
    public async Task<IActionResult> GetPaymentResult(string sessionId, CancellationToken ct = default)
    {
        try
        {
            var transaction = await _paymentService.GetPaymentResultAsync(sessionId, ct);
            
            if (transaction == null)
            {
                return NotFound(new { message = $"Không tìm thấy giao dịch với sessionId: {sessionId}" });
            }

            return Ok(transaction);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Lỗi khi lấy kết quả thanh toán", error = ex.Message });
        }
    }
}

