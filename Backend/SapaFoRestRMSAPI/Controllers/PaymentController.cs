using BusinessAccessLayer.DTOs.Payment;
using BusinessAccessLayer.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace SapaFoRestRMSAPI.Controllers;

/// <summary>
/// Controller xử lý các API thanh toán
/// Owner/Manager/Staff: Xử lý thanh toán
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Owner,Manager,Staff")]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly IConfiguration _configuration;
    private readonly IReceiptService _receiptService;
    private readonly IWebHostEnvironment _env;

    public PaymentController(IPaymentService paymentService, IConfiguration configuration, IReceiptService receiptService, IWebHostEnvironment env)
    {
        _paymentService = paymentService;
        _configuration = configuration;
        _receiptService = receiptService;
        _env = env;
    }

    /// <summary>
    /// Owner/Manager/Staff: Lấy danh sách đơn hàng chờ thanh toán
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
    /// Owner/Manager/Staff: Lấy chi tiết đơn hàng
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
    /// Owner/Manager/Staff: Lấy tóm tắt đơn hàng cho payment screen
    /// GET /api/payment/order/{orderId}
    /// Step 1-2: Load order summary with calculated totals
    /// </summary>
    [HttpGet("order/{orderId}")]
    public async Task<IActionResult> GetOrderSummary(int orderId, CancellationToken ct = default)
    {
        try
        {
            var order = await _paymentService.GetOrderDetailAsync(orderId, ct);
            
            if (order == null)
            {
                return NotFound(new { message = $"Không tìm thấy đơn hàng với ID: {orderId}" });
            }

            // Return order summary with calculated totals
            // Step 2: total = subtotal + tax + serviceFee - discount
            var itemsList = order.OrderItems != null && order.OrderItems.Any()
                ? order.OrderItems.Select(item => new
                {
                    name = item.MenuItemName,
                    quantity = item.Quantity,
                    price = item.UnitPrice,
                    total = item.TotalPrice
                }).Cast<object>().ToList()
                : new List<object>();

            var summary = new
            {
                orderId = order.OrderId,
                orderCode = order.OrderCode,
                subtotal = order.Subtotal ?? 0,
                tax = order.VatAmount ?? 0,
                serviceFee = order.ServiceFee ?? 0,
                discount = order.DiscountAmount ?? 0,
                total = order.TotalAmount ?? 0,
                items = itemsList
            };

            return Ok(summary);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Lỗi khi lấy tóm tắt đơn hàng", error = ex.Message });
        }
    }

    /// <summary>
    /// Owner/Manager/Staff: Áp dụng ưu đãi/giảm giá
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
    /// Owner/Manager/Staff: Khởi tạo giao dịch thanh toán
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
    /// Owner/Manager/Staff: Xác nhận thanh toán
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
    /// Owner/Manager/Staff: Lấy kết quả thanh toán theo sessionId
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

    /// <summary>
    /// Owner/Manager/Staff: Tạo VietQR cho đơn hàng
    /// GET /api/payment/vietqr/{orderId}?amount={optionalAmount}
    /// </summary>
    [HttpGet("vietqr/{orderId}")]
    public async Task<IActionResult> GenerateVietQR(int orderId, [FromQuery] decimal? amount = null, CancellationToken ct = default)
    {
        try
        {
            // Lấy cấu hình từ appsettings.json
            var bankCode = _configuration["BankSettings:BankCode"] ?? "VCB";
            var account = _configuration["BankSettings:Account"] ?? "0123456789";

            var qrResponse = await _paymentService.GenerateVietQRAsync(orderId, bankCode, account, amount, ct);
            
            return Ok(new
            {
                qrUrl = qrResponse.QrUrl,
                orderId = qrResponse.OrderId,
                total = qrResponse.Total,
                orderCode = qrResponse.OrderCode,
                description = qrResponse.Description
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Lỗi khi tạo VietQR", error = ex.Message });
        }
    }

    /// <summary>
    /// Owner/Manager/Staff: Xác nhận thanh toán VietQR
    /// POST /api/payment/vietqr/{orderId}/confirm
    /// </summary>
    [HttpPost("vietqr/{orderId}/confirm")]
    public async Task<IActionResult> ConfirmVietQRPayment(int orderId, CancellationToken ct = default)
    {
        try
        {
            // Lấy thông tin order để tính tổng tiền
            var order = await _paymentService.GetOrderDetailAsync(orderId, ct);
            if (order == null)
            {
                return NotFound(new { message = $"Không tìm thấy đơn hàng với ID: {orderId}" });
            }

            var totalAmount = order.TotalAmount ?? 0;

            // Tạo payment request với phương thức VietQR
            var paymentRequest = new PaymentRequestDto
            {
                OrderId = orderId,
                PaymentMethod = "VietQR",
                Amount = totalAmount,
                Notes = "Thanh toán qua VietQR"
            };

            var transaction = await _paymentService.ProcessPaymentAsync(paymentRequest, ct);
            
            return Ok(new
            {
                success = true,
                message = "Xác nhận thanh toán thành công",
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
            return StatusCode(500, new { message = "Lỗi khi xác nhận thanh toán", error = ex.Message });
        }
    }

    // ========== PHASE 1: Payment Flow Extensions ==========

    /// <summary>
    /// Owner/Manager/Staff: Xử lý thanh toán tiền mặt
    /// POST /api/payment/cash
    /// </summary>
    [HttpPost("cash")]
    public async Task<IActionResult> ProcessCashPayment([FromBody] CashPaymentRequestDto request, CancellationToken ct = default)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = GetUserIdFromClaims();
            if (userId == null)
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            var transaction = await _paymentService.ProcessCashPaymentAsync(request, userId.Value, ct);
            return Ok(transaction);
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
            return StatusCode(500, new { message = "Lỗi khi xử lý thanh toán tiền mặt", error = ex.Message });
        }
    }

    /// <summary>
    /// Owner/Manager/Staff: Kiểm tra trạng thái thanh toán
    /// GET /api/payment/status/{orderId}
    /// </summary>
    [HttpGet("status/{orderId}")]
    public async Task<IActionResult> CheckPaymentStatus(int orderId, CancellationToken ct = default)
    {
        try
        {
            var status = await _paymentService.CheckPaymentStatusAsync(orderId, ct);
            return Ok(status);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Lỗi khi kiểm tra trạng thái thanh toán", error = ex.Message });
        }
    }

    /// <summary>
    /// Owner/Manager/Staff: Retry payment
    /// POST /api/payment/retry
    /// </summary>
    [HttpPost("retry")]
    public async Task<IActionResult> RetryPayment([FromBody] PaymentRetryRequestDto request, CancellationToken ct = default)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = GetUserIdFromClaims();
            if (userId == null)
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            var transaction = await _paymentService.RetryPaymentAsync(request, userId.Value, ct);
            return Ok(transaction);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Lỗi khi retry payment", error = ex.Message });
        }
    }

    /// <summary>
    /// Owner/Manager/Staff: Sync offline payments
    /// POST /api/payment/sync
    /// </summary>
    [HttpPost("sync")]
    public async Task<IActionResult> SyncPayments([FromBody] SyncPaymentsRequestDto request, CancellationToken ct = default)
    {
        try
        {
            if (request.TransactionIds == null || !request.TransactionIds.Any())
            {
                return BadRequest(new { message = "TransactionIds không được để trống" });
            }

            var transactions = await _paymentService.SyncPaymentsAsync(request.TransactionIds, ct);
            return Ok(transactions);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Lỗi khi sync payments", error = ex.Message });
        }
    }

    /// <summary>
    /// Gateway callback notification
    /// SIMPLIFIED: Not used in Cash/QR manual confirmation system
    /// Kept for backward compatibility but returns not implemented
    /// </summary>
    [HttpPost("notify")]
    [AllowAnonymous]
    [Obsolete("Gateway callbacks not used in simplified Cash/QR payment system")]
    public async Task<IActionResult> NotifyPayment([FromBody] PaymentNotifyRequestDto request, CancellationToken ct = default)
    {
        // Simplified payment system: No gateway callbacks needed
        // Cash and QR payments use manual confirmation only
        return BadRequest(new { message = "Gateway callbacks not supported in simplified payment system. Use manual confirmation instead." });
    }

    /// <summary>
    /// Owner/Manager/Staff: Lock order
    /// POST /api/payment/lock
    /// </summary>
    [HttpPost("lock")]
    public async Task<IActionResult> LockOrder([FromBody] OrderLockRequestDto request, CancellationToken ct = default)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = GetUserIdFromClaims();
            if (userId == null)
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            var success = await _paymentService.LockOrderAsync(request, userId.Value, ct);
            return Ok(new { success, message = "Order locked successfully" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Lỗi khi lock order", error = ex.Message });
        }
    }

    /// <summary>
    /// Owner/Manager/Staff: Unlock order
    /// POST /api/payment/unlock/{orderId}
    /// </summary>
    [HttpPost("unlock/{orderId}")]
    public async Task<IActionResult> UnlockOrder(int orderId, CancellationToken ct = default)
    {
        try
        {
            var success = await _paymentService.UnlockOrderAsync(orderId, ct);
            return Ok(new { success, message = "Order unlocked successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Lỗi khi unlock order", error = ex.Message });
        }
    }

    /// <summary>
    /// Owner/Manager/Staff: Check if order is locked
    /// GET /api/payment/lock/{orderId}
    /// </summary>
    [HttpGet("lock/{orderId}")]
    public async Task<IActionResult> IsOrderLocked(int orderId, CancellationToken ct = default)
    {
        try
        {
            var isLocked = await _paymentService.IsOrderLockedAsync(orderId, ct);
            return Ok(new { isLocked });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Lỗi khi kiểm tra lock status", error = ex.Message });
        }
    }

    /// <summary>
    /// Owner/Manager/Staff: Split bill
    /// POST /api/payment/split-bill
    /// </summary>
    [HttpPost("split-bill")]
    public async Task<IActionResult> SplitBill([FromBody] SplitBillRequestDto request, CancellationToken ct = default)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = GetUserIdFromClaims();
            if (userId == null)
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            var transactions = await _paymentService.ProcessSplitBillAsync(request, userId.Value, ct);
            return Ok(transactions);
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
            return StatusCode(500, new { message = "Lỗi khi chia hóa đơn", error = ex.Message });
        }
    }

    // ========== QR VIETQR MANUAL CONFIRMATION ==========

    /// <summary>
    /// Owner/Manager/Staff: Tạo VietQR cho thanh toán thủ công
    /// GET /api/payment/qr/{orderId}
    /// 
    /// Integration: Frontend calls this when cashier clicks "Thanh toán QR"
    /// Returns: { qrUrl, amount, description, orderId, orderCode, transactionId, transactionCode }
    /// </summary>
    [HttpGet("qr/{orderId}")]
    public async Task<IActionResult> GenerateQRForManualConfirmation(int orderId, CancellationToken ct = default)
    {
        try
        {
            // Step 1: Get bank configuration from appsettings.json
            var bankCode = _configuration["BankSettings:BankCode"] ?? "VCB";
            var account = _configuration["BankSettings:Account"] ?? "0123456789";

            // Step 2: Validate order exists
            var order = await _paymentService.GetOrderDetailAsync(orderId, ct);
            if (order == null)
            {
                return NotFound(new { message = $"Không tìm thấy đơn hàng với ID: {orderId}" });
            }

            // Step 3: Start payment flow - creates transaction with status "PaymentProcessing"
            var transaction = await _paymentService.StartPaymentAsync(orderId, "QRBankTransfer", ct);

            // Step 4: Generate VietQR URL using bank settings and order details
            var qrResponse = await _paymentService.GenerateVietQRAsync(orderId, bankCode, account, null, ct);

            // Step 5: Return QR data for frontend to display
            // Response structure matches frontend expectations
            return Ok(new
            {
                qrUrl = qrResponse.QrUrl,              // QR image URL
                amount = qrResponse.Total,              // Total amount to pay
                description = qrResponse.Description,   // Transfer description (e.g., "RMS#ORD1024")
                orderId = qrResponse.OrderId,           // Order ID for confirmation
                orderCode = qrResponse.OrderCode,       // Order code for display
                transactionId = transaction.TransactionId,     // Transaction ID for confirmation
                transactionCode = transaction.TransactionCode   // Transaction code for display
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
            return StatusCode(500, new { message = "Lỗi khi tạo VietQR", error = ex.Message });
        }
    }

    // ========== REVISED PAYMENT WORKFLOW ENDPOINTS ==========

    /// <summary>
    /// Owner/Manager/Staff: Bắt đầu thanh toán
    /// GET /api/payments/start/{orderId}?paymentMethod={method}
    /// </summary>
    [HttpGet("start/{orderId}")]
    public async Task<IActionResult> StartPayment(
        int orderId,
        [FromQuery] string paymentMethod,
        CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrEmpty(paymentMethod))
            {
                return BadRequest(new { message = "PaymentMethod là bắt buộc" });
            }

            var transaction = await _paymentService.StartPaymentAsync(orderId, paymentMethod, ct);
            return Ok(transaction);
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
            return StatusCode(500, new { message = "Lỗi khi bắt đầu thanh toán", error = ex.Message });
        }
    }

    /// <summary>
    /// Owner/Manager/Staff: Xác nhận thanh toán
    /// POST /api/payments/confirm
    /// 
    /// Integration: Frontend calls this after cashier confirms payment manually
    /// Expected request: { orderId, transactionId, gatewayReference?, notes? }
    /// Returns: { success: true, message: "...", transaction: {...} }
    /// </summary>
    [HttpPost("confirm")]
    public async Task<IActionResult> ConfirmPayment([FromBody] PaymentConfirmRequestDto request, CancellationToken ct = default)
    {
        try
        {
            // Validate request model
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Get authenticated user ID
            var userId = GetUserIdFromClaims();
            if (userId == null)
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            // Confirm payment in backend - updates order status to PAID
            var transaction = await _paymentService.ConfirmManualAsync(request, userId.Value, ct);
            
            // Return success response for frontend
            return Ok(new
            {
                success = true,
                message = "Xác nhận thanh toán thành công",
                status = "PAID",
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
            return StatusCode(500, new { message = "Lỗi khi xác nhận thanh toán", error = ex.Message });
        }
    }

    /// <summary>
    /// Owner/Manager/Staff: Hủy thanh toán
    /// POST /api/payments/cancel
    /// </summary>
    [HttpPost("cancel")]
    public async Task<IActionResult> CancelPayment([FromBody] PaymentCancelRequestDto request, CancellationToken ct = default)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = GetUserIdFromClaims();
            if (userId == null)
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            var success = await _paymentService.CancelPaymentAsync(request, userId.Value, ct);
            return Ok(new
            {
                success = success,
                message = "Hủy thanh toán thành công"
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
            return StatusCode(500, new { message = "Lỗi khi hủy thanh toán", error = ex.Message });
        }
    }

    /// <summary>
    /// Owner/Manager/Staff: Kiểm tra trạng thái thanh toán (revised endpoint)
    /// GET /api/payments/check-status/{orderId}
    /// </summary>
    [HttpGet("check-status/{orderId}")]
    public async Task<IActionResult> CheckPaymentStatusRevised(int orderId, CancellationToken ct = default)
    {
        try
        {
            var status = await _paymentService.CheckPaymentStatusAsync(orderId, ct);
            return Ok(status);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Lỗi khi kiểm tra trạng thái thanh toán", error = ex.Message });
        }
    }

    /// <summary>
    /// Gateway callback notification
    /// SIMPLIFIED: Not used in Cash/QR manual confirmation system
    /// Removed - use manual confirmation endpoints instead
    /// </summary>
    [HttpPost("notify-callback")]
    [AllowAnonymous]
    [Obsolete("Gateway callbacks not used in simplified Cash/QR payment system")]
    public async Task<IActionResult> NotifyPaymentRevised([FromBody] PaymentNotifyRequestDto request, CancellationToken ct = default)
    {
        // Simplified payment system: No gateway callbacks needed
        return BadRequest(new { message = "Gateway callbacks not supported. Use POST /api/payment/confirm for manual confirmation." });
    }

    /// <summary>
    /// Owner/Manager/Staff: Download receipt PDF for a paid order
    /// GET /api/payment/receipt/{orderId}
    /// </summary>
    [HttpGet("receipt/{orderId}")]
    public async Task<IActionResult> GetReceipt(int orderId, CancellationToken ct = default)
    {
        try
        {
            // Get order to verify it exists and is paid
            var order = await _paymentService.GetOrderDetailAsync(orderId, ct);
            if (order == null)
            {
                return NotFound(new { message = $"Không tìm thấy đơn hàng với ID: {orderId}" });
            }

            // Check if order is paid
            if (order.Status != "Paid" && order.Status != "PAID")
            {
                return BadRequest(new { message = $"Đơn hàng chưa được thanh toán. Trạng thái hiện tại: {order.Status}" });
            }

            // Generate order code
            var orderCode = order.OrderCode ?? $"RMS{orderId:D6}";
            var pdfFileName = $"{orderCode}.pdf";
            var pdfPath = Path.Combine(_env.WebRootPath, "receipts", pdfFileName);

            // Check if PDF exists, if not generate it
            if (!System.IO.File.Exists(pdfPath))
            {
                // Generate receipt
                await _receiptService.GenerateReceiptPdfAsync(orderId, ct);
            }

            // Verify file exists after generation
            if (!System.IO.File.Exists(pdfPath))
            {
                return NotFound(new { message = "Không thể tạo hóa đơn. Vui lòng thử lại." });
            }

            // Return PDF file
            var fileBytes = await System.IO.File.ReadAllBytesAsync(pdfPath, ct);
            return File(fileBytes, "application/pdf", pdfFileName);
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
            return StatusCode(500, new { message = "Lỗi khi tải hóa đơn", error = ex.Message });
        }
    }

    // Helper method to get user ID from claims
    private int? GetUserIdFromClaims()
    {
        var userIdClaim = User.FindFirst("userId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return null;
        }
        return userId;
    }
}

// DTO for sync payments request
public class SyncPaymentsRequestDto
{
    public List<int> TransactionIds { get; set; } = new List<int>();
}

