using Microsoft.AspNetCore.Mvc;
using WebSapaForestForStaff.DTOs.Payment;
using WebSapaForestForStaff.Services.Api.Interfaces;
using WebSapaForestForStaff.ViewModels.Payment;
using System.Text.Json;
using System.Net.Http.Headers;

namespace WebSapaForestForStaff.Controllers
{
    [Route("cashier-flow")]
    public class CashierPaymentFlowController : Controller
    {
        private readonly IPaymentApiService _paymentApiService;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CashierPaymentFlowController(
            IPaymentApiService paymentApiService,
            HttpClient httpClient,
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor)
        {
            _paymentApiService = paymentApiService;
            _httpClient = httpClient;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
        }

        private string GetApiBaseUrl() => _configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7096/api";

        private string? GetToken()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null) return null;
            var tokenFromSession = httpContext.Session.GetString("Token");
            if (!string.IsNullOrEmpty(tokenFromSession)) return tokenFromSession;
            return httpContext.User?.FindFirst("Token")?.Value;
        }

        [HttpGet("orders")]
        public async Task<IActionResult> OrderSelection(DateOnly? date = null)
        {
            var selectedDate = date ?? DateOnly.FromDateTime(DateTime.Now);
            var pending = await _paymentApiService.GetOrdersByStatusAndDateAsync("pending", selectedDate) ?? new List<OrderDto>();
            var paid = await _paymentApiService.GetOrdersByStatusAndDateAsync("processed", selectedDate) ?? new List<OrderDto>();

            var viewModel = new OrderSelectionViewModel
            {
                SelectedDate = selectedDate,
                PendingOrders = pending,
                PaidOrders = paid
            };

            // Set ViewBag for partial view (defaults to pending for initial load)
            ViewBag.OrderStatus = "pending";

            return View("~/Views/CashierFlow/OrderSelection.cshtml", viewModel);
        }

        [HttpGet("orders/partial")]
        public async Task<IActionResult> LoadOrdersPartial(DateOnly date, string status = "pending")
        {
            var orders = await _paymentApiService.GetOrdersByStatusAndDateAsync(status, date) ?? new List<OrderDto>();
            ViewBag.OrderStatus = status;
            return PartialView("~/Views/CashierFlow/_OrderListPartial.cshtml", orders);
        }

        // ❌ REMOVED: OrderDetail, CustomerConfirm, ConfirmOrder
        // Thu ngân KHÔNG xác nhận món nữa
        // Waiter đã xác nhận món trước khi chuyển sang thanh toán

        [HttpGet("payment/{id}")]
        public async Task<IActionResult> Payment(int id)
        {
            // Khi vào màn hình thanh toán, xoá mọi ErrorMessage/SuccessMessage cũ (ví dụ từ UserProfile)
            TempData.Remove("ErrorMessage");
            TempData.Remove("SuccessMessage");

            var order = await _paymentApiService.GetOrderDetailAsync(id);
            if (order == null) return NotFound();
            
            // ✅ Load danh sách voucher phù hợp với đơn hàng hiện tại
            var availableVouchers = await GetAvailableVouchersAsync(order.Subtotal);
            ViewData["AvailableVouchers"] = availableVouchers;
            
            // ✅ MỚI: Thu ngân KHÔNG validate confirm
            // Waiter đã xác nhận món trước đó
            // Thu ngân chỉ xử lý thanh toán
            
            return View("~/Views/CashierFlow/Payment.cshtml", order);
        }

        /// <summary>
        /// Lấy danh sách voucher phù hợp với đơn hàng (status="Đang sử dụng", minOrderValue <= subtotal)
        /// </summary>
        private async Task<List<VoucherDto>> GetAvailableVouchersAsync(decimal subtotal)
        {
            try
            {
                var token = GetToken();
                if (string.IsNullOrEmpty(token)) return new List<VoucherDto>();

                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                
                // Gọi API voucher với filter: status="Đang sử dụng", minOrderValue <= subtotal
                var url = $"{GetApiBaseUrl()}/Voucher?status=Đang sử dụng&minOrderValue=0&pageNumber=1&pageSize=50";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode) return new List<VoucherDto>();

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<JsonElement>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (!result.TryGetProperty("Data", out var dataElement)) return new List<VoucherDto>();

                var vouchers = JsonSerializer.Deserialize<List<VoucherDto>>(dataElement.GetRawText(), new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new List<VoucherDto>();

                // Lọc lại: chỉ lấy voucher có MinOrderValue <= subtotal (hoặc không có MinOrderValue)
                return vouchers
                    .Where(v => !(v.IsDelete ?? false))
                    .Where(v => !v.MinOrderValue.HasValue || v.MinOrderValue.Value <= subtotal)
                    .OrderByDescending(v => v.DiscountValue) // Sắp xếp theo giá trị giảm giá giảm dần
                    .ToList();
            }
            catch
            {
                return new List<VoucherDto>();
            }
        }

        public class VoucherDto
        {
            public int VoucherId { get; set; }
            public string Code { get; set; } = string.Empty;
            public string? Description { get; set; }
            public string? DiscountType { get; set; }
            public decimal DiscountValue { get; set; }
            public DateTime? StartDate { get; set; }
            public DateTime? EndDate { get; set; }
            public decimal? MinOrderValue { get; set; }
            public decimal? MaxDiscount { get; set; }
            public string? Status { get; set; }
            public bool? IsDelete { get; set; }
        }

        [HttpPost("payment/initiate")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> InitiatePayment(PaymentInitiateRequest request)
        {
            if (request.OrderId <= 0 || string.IsNullOrEmpty(request.PaymentMethod))
            {
                TempData["ErrorMessage"] = "Vui lòng chọn phương thức thanh toán.";
                return RedirectToAction(nameof(Payment), new { id = request.OrderId });
            }

            // ✅ KHÔNG cần validate confirm ở đây – waiter đã xác nhận trước đó (OrderDetail flow)
            var order = await _paymentApiService.GetOrderDetailAsync(request.OrderId);
            if (order == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn hàng.";
                return RedirectToAction(nameof(OrderSelection));
            }

            try
            {
                var session = await _paymentApiService.InitiatePaymentAsync(request);
                if (session == null || string.IsNullOrEmpty(session.SessionId))
                {
                    TempData["ErrorMessage"] = "Không thể khởi tạo thanh toán.";
                    return RedirectToAction(nameof(Payment), new { id = request.OrderId });
                }

                var viewModel = new PaymentConfirmViewModel
                {
                    OrderId = request.OrderId,
                    Session = session
                };

                return View("~/Views/CashierFlow/PaymentConfirm.cshtml", viewModel);
            }
            catch (InvalidOperationException ex)
            {
                // Lỗi business từ API (ví dụ: Đơn hàng chưa được khách xác nhận)
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(Payment), new { id = request.OrderId });
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi khởi tạo thanh toán. Vui lòng thử lại.";
                return RedirectToAction(nameof(Payment), new { id = request.OrderId });
            }
        }

        [HttpPost("payment/confirm")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmPayment(PaymentConfirmRequest request)
        {
            var result = await _paymentApiService.ConfirmPaymentAsync(request);
            if (!result.Success)
            {
                TempData["ErrorMessage"] = result.Message;
                var session = new PaymentSessionDto { SessionId = request.SessionId };
                var viewModel = new PaymentConfirmViewModel
                {
                    OrderId = request.OrderId,
                    Session = session
                };
                return View("~/Views/CashierFlow/PaymentConfirm.cshtml", viewModel);
            }

            TempData["SuccessMessage"] = result.Message;
            return RedirectToAction(nameof(Receipt), new { orderId = request.OrderId });
        }

        /// <summary>
        /// POST: Xử lý thanh toán tiền mặt
        /// </summary>
        [HttpPost("payment/cash")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessCashPayment(CashPaymentRequest request)
        {
            if (request == null || request.OrderId <= 0)
            {
                TempData["ErrorMessage"] = "Dữ liệu thanh toán không hợp lệ.";
                return RedirectToAction(nameof(Payment), new { id = request?.OrderId ?? 0 });
            }

            try
            {
                var token = GetToken();
                if (string.IsNullOrEmpty(token))
                {
                    TempData["ErrorMessage"] = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";
                    return RedirectToAction("Login", "Auth");
                }

                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var apiUrl = $"{GetApiBaseUrl()}/Payment/cash";
                var response = await _httpClient.PostAsJsonAsync(apiUrl, new
                {
                    orderId = request.OrderId,
                    amountReceived = request.AmountReceived,
                    notes = request.Notes
                });

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    var errorData = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(errorContent, new System.Text.Json.JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    var errorMessage = errorData.TryGetProperty("message", out var msg) 
                        ? msg.GetString() 
                        : "Không thể xử lý thanh toán. Vui lòng thử lại.";

                    TempData["ErrorMessage"] = errorMessage;
                    return RedirectToAction(nameof(Payment), new { id = request.OrderId });
                }

                var transaction = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
                var refundAmount = transaction.TryGetProperty("refundAmount", out var refund) && refund.ValueKind == System.Text.Json.JsonValueKind.Number
                    ? refund.GetDecimal()
                    : (decimal?)null;

                if (refundAmount.HasValue && refundAmount.Value > 0)
                {
                    TempData["SuccessMessage"] = $"✅ Thanh toán thành công! Đã trả lại tiền thừa: {refundAmount.Value:N0} ₫";
                }
                else
                {
                    TempData["SuccessMessage"] = "✅ Thanh toán thành công!";
                }

                return RedirectToAction(nameof(Receipt), new { orderId = request.OrderId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi khi xử lý thanh toán: {ex.Message}";
                return RedirectToAction(nameof(Payment), new { id = request.OrderId });
            }
        }

        [HttpGet("receipt/{orderId}")]
        public async Task<IActionResult> Receipt(int orderId)
        {
            var order = await _paymentApiService.GetOrderDetailAsync(orderId);
            if (order == null) return NotFound();
            return View("~/Views/CashierFlow/Receipt.cshtml", order);
        }

        [HttpGet("receipt/{orderId}/download")]
        public async Task<IActionResult> DownloadReceipt(int orderId)
        {
            var file = await _paymentApiService.GenerateReceiptAsync(orderId);
            if (file == null || !file.Success || file.FileBytes.Length == 0)
            {
                TempData["ErrorMessage"] = file?.ErrorMessage ?? "Không thể tải hóa đơn.";
                return RedirectToAction(nameof(Receipt), new { orderId });
            }

            return File(file.FileBytes, "application/pdf", file.FileName);
        }

        /// <summary>
        /// POST: Áp dụng ưu đãi / mã giảm giá cho đơn hàng hiện tại
        /// Form submit từ Promotion Modal → reload lại Payment view với order đã cập nhật
        /// </summary>
        [HttpPost("payment/apply-discount")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApplyDiscount(DiscountRequest request)
        {
            if (request == null || request.OrderId <= 0)
            {
                TempData["DiscountErrorMessage"] = "Dữ liệu ưu đãi không hợp lệ.";
                return RedirectToAction(nameof(Payment), new { id = request?.OrderId ?? 0 });
            }

            // Gọi ApiService để áp dụng voucher
            var result = await _paymentApiService.ApplyDiscountAsync(request);
            
            if (result == null)
            {
                TempData["DiscountErrorMessage"] = "Không thể áp dụng ưu đãi. Vui lòng thử lại sau.";
                TempData["VoucherCode"] = request.VoucherCode; // Giữ lại mã đã nhập để user sửa
                return RedirectToAction(nameof(Payment), new { id = request.OrderId });
            }

            if (!result.Success)
            {
                TempData["DiscountErrorMessage"] = result.Message;
                TempData["VoucherCode"] = request.VoucherCode; // Giữ lại mã đã nhập
                return RedirectToAction(nameof(Payment), new { id = request.OrderId });
            }

            // Thành công: reload lại Payment view với order đã cập nhật
            TempData["DiscountSuccessMessage"] = result.Message ?? "Áp dụng ưu đãi thành công!";
            
            // Reload lại order từ API để có dữ liệu mới nhất
            var updatedOrder = await _paymentApiService.GetOrderDetailAsync(request.OrderId);
            if (updatedOrder == null)
            {
                TempData["ErrorMessage"] = "Không thể tải lại thông tin đơn hàng sau khi áp dụng ưu đãi.";
                return RedirectToAction(nameof(OrderSelection));
            }

            // Load lại danh sách voucher phù hợp
            var availableVouchers = await GetAvailableVouchersAsync(updatedOrder.Subtotal);
            ViewData["AvailableVouchers"] = availableVouchers;

            // Return Payment view với order đã cập nhật
            return View("~/Views/CashierFlow/Payment.cshtml", updatedOrder);
        }
    }
}

