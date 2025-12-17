using Microsoft.AspNetCore.Mvc;
using WebSapaForestForStaff.DTOs.Payment;
using WebSapaForestForStaff.Services.Api.Interfaces;
using WebSapaForestForStaff.ViewModels.Payment;
using System.Text.Json;
using System.Net.Http.Headers;
using WebSapaForestForStaff.Models.VoucherDTO;

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

        public record ConfirmQrPaymentRequest(int OrderId, string? Notes);
        
        public class SplitBillPart
        {
            public string PaymentMethod { get; set; } = "Cash";
            public decimal Amount { get; set; }
            public decimal? AmountReceived { get; set; }
            public string? Notes { get; set; }
        }

        [HttpGet("orders")]
        public async Task<IActionResult> OrderSelection(DateOnly? date = null, string status = "Confirmed")
        {
            var selectedDate = date ?? DateOnly.FromDateTime(DateTime.Now);

            // Normalize status
            var normalizedStatus = status?.Equals("Paid", StringComparison.OrdinalIgnoreCase) == true
                ? "Paid"
                : "Confirmed";

            // ✅ DEBUG: Log để kiểm tra
            System.Diagnostics.Debug.WriteLine($"[OrderSelection] Date: {selectedDate}, Status: {status}, Normalized: {normalizedStatus}");

            // Confirmed: waiter đã xác nhận, chờ thu ngân thanh toán
            var confirmedOrders = await _paymentApiService.GetOrdersByStatusAndDateAsync("Confirmed", selectedDate) ?? new List<OrderDto>();
            
            // ✅ DEBUG: Log số lượng orders
            System.Diagnostics.Debug.WriteLine($"[OrderSelection] Confirmed orders count: {confirmedOrders.Count}");
            foreach (var order in confirmedOrders.Take(5))
            {
                System.Diagnostics.Debug.WriteLine($"[OrderSelection] Confirmed Order {order.OrderId}: Status = {order.Status}");
            }

            // Paid: đã thanh toán xong
            var paidOrders = await _paymentApiService.GetOrdersByStatusAndDateAsync("Paid", selectedDate) ?? new List<OrderDto>();
            
            // ✅ DEBUG: Log số lượng orders
            System.Diagnostics.Debug.WriteLine($"[OrderSelection] Paid orders count: {paidOrders.Count}");
            foreach (var order in paidOrders.Take(5))
            {
                System.Diagnostics.Debug.WriteLine($"[OrderSelection] Paid Order {order.OrderId}: Status = {order.Status}");
            }

            var viewModel = new OrderSelectionViewModel
            {
                SelectedDate = selectedDate,
                PendingOrders = confirmedOrders,
                PaidOrders = paidOrders
            };

            // Set ViewBag for partial view
            // Sử dụng lowercase để match với logic trong partial view
            ViewBag.OrderStatus = normalizedStatus.Equals("Paid", StringComparison.OrdinalIgnoreCase) ? "paid" : "confirmed";
            ViewBag.CurrentStatus = normalizedStatus; // Store original status for view

            return View("~/Views/CashierFlow/OrderSelection.cshtml", viewModel);
        }

        /// <summary>
        /// ✅ DEBUG: Endpoint để test API trực tiếp
        /// </summary>
        [HttpGet("orders/debug")]
        public async Task<IActionResult> DebugOrders([FromQuery] DateOnly? date = null, [FromQuery] string status = "Confirmed")
        {
            var selectedDate = date ?? DateOnly.FromDateTime(DateTime.Now);
            var normalizedStatus = status?.Equals("Paid", StringComparison.OrdinalIgnoreCase) == true ? "Paid" : "Confirmed";
            
            var orders = await _paymentApiService.GetOrdersByStatusAndDateAsync(normalizedStatus, selectedDate) ?? new List<OrderDto>();
            
            return Json(new
            {
                date = selectedDate.ToString("yyyy-MM-dd"),
                requestedStatus = status,
                normalizedStatus = normalizedStatus,
                ordersCount = orders.Count,
                orders = orders.Select(o => new
                {
                    orderId = o.OrderId,
                    orderCode = o.OrderCode,
                    status = o.Status,
                    createdAt = o.CreatedAt,
                    totalAmount = o.TotalAmount
                }).ToList()
            });
        }

        [HttpGet("orders/partial")]
        public async Task<IActionResult> LoadOrdersPartial(DateOnly date, string status = "Confirmed")
        {
            // Normalize status to backend contract
            var normalizedStatus = status?.Equals("paid", StringComparison.OrdinalIgnoreCase) == true
                ? "Paid"
                : "Confirmed";

            var orders = await _paymentApiService.GetOrdersByStatusAndDateAsync(normalizedStatus, date) ?? new List<OrderDto>();
            
            // Set ViewBag để partial view biết đang hiển thị tab nào
            // Sử dụng "confirmed" hoặc "paid" (lowercase) để match với logic trong partial view
            ViewBag.OrderStatus = normalizedStatus.Equals("Paid", StringComparison.OrdinalIgnoreCase) ? "paid" : "confirmed";
            
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
        /// Thu ngân xác nhận đã nhận tiền QR (manual confirm) và trả về redirect URL
        /// </summary>
        [HttpPost("payment/confirm-qr")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmQrPayment([FromForm] ConfirmQrPaymentRequest request)
        {
            // ✅ DEBUG: Log request
            System.Diagnostics.Debug.WriteLine($"[ConfirmQrPayment] Called with OrderId: {request?.OrderId}, Notes: {request?.Notes}");
            
            if (request == null || request.OrderId <= 0)
            {
                System.Diagnostics.Debug.WriteLine($"[ConfirmQrPayment] Invalid request: OrderId = {request?.OrderId}");
                TempData["ErrorMessage"] = "Dữ liệu không hợp lệ";
                return RedirectToAction(nameof(Payment), new { id = request?.OrderId ?? 0 });
            }

            try
            {
                var order = await _paymentApiService.GetOrderDetailAsync(request.OrderId);
                if (order == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[ConfirmQrPayment] Order not found: {request.OrderId}");
                    TempData["ErrorMessage"] = $"Không tìm thấy đơn hàng {request.OrderId}";
                    return RedirectToAction(nameof(OrderSelection));
                }

                System.Diagnostics.Debug.WriteLine($"[ConfirmQrPayment] Order found: {request.OrderId}, TotalAmount: {order.TotalAmount}, Status: {order.Status}");

                var confirmRequest = new PaymentConfirmRequest
                {
                    OrderId = request.OrderId,
                    PaymentMethod = "QRBankTransfer",
                    Amount = order.TotalAmount,
                    Notes = request.Notes ?? "Thu ngân xác nhận đã nhận tiền qua QR",
                    SessionId = string.Empty,
                    CashGiven = null
                };

                System.Diagnostics.Debug.WriteLine($"[ConfirmQrPayment] Sending confirm request: OrderId={confirmRequest.OrderId}, Amount={confirmRequest.Amount}, PaymentMethod={confirmRequest.PaymentMethod}");
                
                var result = await _paymentApiService.ConfirmPaymentAsync(confirmRequest);
                
                System.Diagnostics.Debug.WriteLine($"[ConfirmQrPayment] API result: Success={result.Success}, Message={result.Message}");
                
                if (!result.Success)
                {
                    var errorMsg = result.Message ?? "Xác nhận thanh toán thất bại";
                    System.Diagnostics.Debug.WriteLine($"[ConfirmQrPayment] Payment failed: {errorMsg}");
                    TempData["ErrorMessage"] = errorMsg;
                    return RedirectToAction(nameof(Payment), new { id = request.OrderId });
                }

                TempData["SuccessMessage"] = "✅ Đã xác nhận thanh toán QR thành công!";
                System.Diagnostics.Debug.WriteLine($"[ConfirmQrPayment] Payment successful, redirecting to Receipt for OrderId: {request.OrderId}");
                return RedirectToAction(nameof(Receipt), new { orderId = request.OrderId });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ConfirmQrPayment] Exception: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[ConfirmQrPayment] StackTrace: {ex.StackTrace}");
                TempData["ErrorMessage"] = $"Lỗi khi xác nhận thanh toán: {ex.Message}";
                return RedirectToAction(nameof(Payment), new { id = request.OrderId });
            }
        }

        /// <summary>
        /// API endpoint để load voucher list qua AJAX
        /// </summary>
        [HttpGet("vouchers/available")]
        public async Task<IActionResult> GetAvailableVouchers([FromQuery] decimal subtotal = 0)
        {
            try
            {
                var vouchers = await GetAvailableVouchersAsync(subtotal);
                return Ok(vouchers);
            }
            catch
            {
                return Ok(new List<VoucherDto>());
            }
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

                // ✅ FIX: API trả về property "Data" (chữ hoa), không phải "data"
                // JsonElement.TryGetProperty là case-sensitive, cần check đúng tên property
                if (!result.TryGetProperty("Data", out var dataElement))
                {
                    // Fallback: thử "data" (chữ thường) nếu "Data" không tồn tại
                    if (!result.TryGetProperty("data", out dataElement))
                    {
                        return new List<VoucherDto>();
                    }
                }

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
            // ✅ DEBUG: Log để trace
            System.Diagnostics.Debug.WriteLine($"[ProcessCashPayment] Called with OrderId: {request?.OrderId}, AmountReceived: {request?.AmountReceived}");
            
            if (request == null || request.OrderId <= 0)
            {
                System.Diagnostics.Debug.WriteLine($"[ProcessCashPayment] Invalid request: OrderId = {request?.OrderId}");
                TempData["ErrorMessage"] = "Dữ liệu thanh toán không hợp lệ.";
                return RedirectToAction(nameof(Payment), new { id = request?.OrderId ?? 0 });
            }

            try
            {
                System.Diagnostics.Debug.WriteLine($"[ProcessCashPayment] Processing payment for OrderId: {request.OrderId}");
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
                    System.Text.Json.JsonElement? errorData = null;
                    
                    try
                    {
                        errorData = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(errorContent, new System.Text.Json.JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                    }
                    catch
                    {
                        // Ignore JSON parse error
                    }

                    var errorMessage = errorData.HasValue && errorData.Value.TryGetProperty("message", out var msg) 
                        ? msg.GetString() 
                        : "Không thể xử lý thanh toán. Vui lòng thử lại.";

                    TempData["ErrorMessage"] = errorMessage ?? "Không thể xử lý thanh toán. Vui lòng thử lại.";
                    return RedirectToAction(nameof(Payment), new { id = request.OrderId });
                }

                // Parse transaction response
                decimal? refundAmount = null;
                try
                {
                    var transaction = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
                    if (transaction.TryGetProperty("refundAmount", out var refund) && refund.ValueKind == System.Text.Json.JsonValueKind.Number)
                    {
                        refundAmount = refund.GetDecimal();
                    }
                }
                catch (Exception parseEx)
                {
                    // Log parse error nhưng vẫn tiếp tục redirect (vì payment đã thành công)
                    // Có thể response không có refundAmount, điều này là bình thường
                }

                if (refundAmount.HasValue && refundAmount.Value > 0)
                {
                    TempData["SuccessMessage"] = $"✅ Thanh toán thành công! Đã trả lại tiền thừa: {refundAmount.Value:N0} ₫";
                }
                else
                {
                    TempData["SuccessMessage"] = "✅ Thanh toán thành công!";
                }

                // ✅ DEBUG: Log trước khi redirect
                System.Diagnostics.Debug.WriteLine($"[ProcessCashPayment] Payment successful, redirecting to Receipt for OrderId: {request.OrderId}");
                
                // Redirect đến Receipt page
                var redirectUrl = Url.Action(nameof(Receipt), new { orderId = request.OrderId });
                System.Diagnostics.Debug.WriteLine($"[ProcessCashPayment] Redirect URL: {redirectUrl}");
                
                return RedirectToAction(nameof(Receipt), new { orderId = request.OrderId });
            }
            catch (Exception ex)
            {
                // ✅ DEBUG: Log exception
                System.Diagnostics.Debug.WriteLine($"[ProcessCashPayment] Exception: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[ProcessCashPayment] StackTrace: {ex.StackTrace}");
                
                TempData["ErrorMessage"] = $"Lỗi khi xử lý thanh toán: {ex.Message}";
                return RedirectToAction(nameof(Payment), new { id = request.OrderId });
            }
        }

        /// <summary>
        /// POST: Xử lý thanh toán kết hợp (Cash + QR)
        /// </summary>
        [HttpPost("payment/combined")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessCombinedPayment(CombinedPaymentRequest request)
        {
            if (request == null || request.OrderId <= 0)
            {
                TempData["ErrorMessage"] = "Dữ liệu thanh toán không hợp lệ.";
                return RedirectToAction(nameof(Payment), new { id = request?.OrderId ?? 0 });
            }

            try
            {
                var result = await _paymentApiService.ProcessCombinedPaymentAsync(request);
                if (!result.Success)
                {
                    TempData["ErrorMessage"] = result.Message;
                    return RedirectToAction(nameof(Payment), new { id = request.OrderId });
                }

                TempData["SuccessMessage"] = result.Message ?? "✅ Thanh toán kết hợp thành công!";
                return RedirectToAction(nameof(Receipt), new { orderId = request.OrderId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi khi xử lý thanh toán kết hợp: {ex.Message}";
                return RedirectToAction(nameof(Payment), new { id = request.OrderId });
            }
        }

        /// <summary>
        /// POST: Xử lý chia hóa đơn
        /// </summary>
        [HttpPost("payment/split-bill")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessSplitBill([FromForm] int orderId, [FromForm] string partsJson, [FromForm] string? notes)
        {
            if (orderId <= 0 || string.IsNullOrEmpty(partsJson))
            {
                TempData["ErrorMessage"] = "Dữ liệu chia hóa đơn không hợp lệ.";
                return RedirectToAction(nameof(Payment), new { id = orderId });
            }

            try
            {
                var token = GetToken();
                if (string.IsNullOrEmpty(token))
                {
                    TempData["ErrorMessage"] = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";
                    return RedirectToAction("Login", "Auth");
                }

                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                // Parse parts from JSON
                var parts = JsonSerializer.Deserialize<List<SplitBillPart>>(partsJson);
                if (parts == null || !parts.Any())
                {
                    TempData["ErrorMessage"] = "Dữ liệu phần chia không hợp lệ.";
                    return RedirectToAction(nameof(Payment), new { id = orderId });
                }

                var apiUrl = $"{GetApiBaseUrl()}/Payment/split-bill";
                var response = await _httpClient.PostAsJsonAsync(apiUrl, new
                {
                    orderId,
                    parts = parts.Select(p => new
                    {
                        paymentMethod = p.PaymentMethod,
                        amount = p.Amount,
                        amountReceived = p.AmountReceived,
                        notes = p.Notes
                    }),
                    notes
                });

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    TempData["ErrorMessage"] = $"Không thể chia hóa đơn: {errorContent}";
                    return RedirectToAction(nameof(Payment), new { id = orderId });
                }

                TempData["SuccessMessage"] = "✅ Đã chia hóa đơn thành công!";
                return RedirectToAction(nameof(Receipt), new { orderId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi khi chia hóa đơn: {ex.Message}";
                return RedirectToAction(nameof(Payment), new { id = orderId });
            }
        }

        [HttpGet("receipt/{orderId}")]
        public async Task<IActionResult> Receipt(int orderId)

        {
            try
            {
                // ✅ DEBUG: Log để trace
                System.Diagnostics.Debug.WriteLine($"[Receipt] Loading order {orderId} for receipt");
                
                // Retry logic: Đợi một chút để đảm bảo database đã commit transaction
                var order = await _paymentApiService.GetOrderDetailAsync(orderId);
                
                // ✅ DEBUG: Log customer info
                if (order != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[Receipt] Order {orderId} - CustomerId: {order.CustomerId}, CustomerName: {order.CustomerName}, CustomerPhone: {order.CustomerPhone}");
                }
                
                // Nếu không tìm thấy, thử lại sau 500ms (có thể do database chưa commit)
                if (order == null)
                {
                    await Task.Delay(500);
                    order = await _paymentApiService.GetOrderDetailAsync(orderId);
                    
                    if (order == null)
                    {
                        TempData["ErrorMessage"] = $"Không tìm thấy đơn hàng với ID: {orderId}";
                        return RedirectToAction(nameof(OrderSelection));
                    }
                }

                // Kiểm tra đơn hàng đã được thanh toán chưa
                // Nếu chưa Paid nhưng có thể đang trong quá trình xử lý, thử lại một lần nữa
                if (string.IsNullOrEmpty(order.Status) || 
                    (!order.Status.Equals("Paid", StringComparison.OrdinalIgnoreCase) &&
                     !order.Status.Equals("Completed", StringComparison.OrdinalIgnoreCase) &&
                     !order.Status.Equals("Success", StringComparison.OrdinalIgnoreCase)))
                {
                    // Đợi thêm một chút và thử lại
                    await Task.Delay(500);
                    order = await _paymentApiService.GetOrderDetailAsync(orderId);
                    
                    if (order != null && 
                        (order.Status.Equals("Paid", StringComparison.OrdinalIgnoreCase) ||
                         order.Status.Equals("Completed", StringComparison.OrdinalIgnoreCase) ||
                         order.Status.Equals("Success", StringComparison.OrdinalIgnoreCase)))
                    {
                        // Status đã được cập nhật, hiển thị receipt
                        return View("~/Views/CashierFlow/Receipt.cshtml", order);
                    }
                    
                    // Vẫn chưa Paid, nhưng có thể đang trong quá trình xử lý
                    // Cho phép xem receipt nếu có thông báo thành công từ TempData
                    if (TempData.ContainsKey("SuccessMessage"))
                    {
                        // Có thông báo thành công, cho phép xem receipt
                        return View("~/Views/CashierFlow/Receipt.cshtml", order);
                    }
                    
                    TempData["ErrorMessage"] = $"Đơn hàng chưa được thanh toán. Trạng thái hiện tại: {order?.Status ?? "N/A"}";
                    return RedirectToAction(nameof(Payment), new { id = orderId });
                }

                return View("~/Views/CashierFlow/Receipt.cshtml", order);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi khi tải thông tin đơn hàng: {ex.Message}";
                return RedirectToAction(nameof(OrderSelection));
            }
        }

        [HttpGet("receipt/{orderId}/download")]
        public async Task<IActionResult> DownloadReceipt(int orderId)
        {
            try
            {
                var file = await _paymentApiService.GenerateReceiptAsync(orderId);
                
                if (file == null)
                {
                    TempData["ErrorMessage"] = "Không thể tải hóa đơn. Vui lòng thử lại sau.";
                    return RedirectToAction(nameof(Receipt), new { orderId });
                }

                if (!file.Success)
                {
                    // Log error message chi tiết
                    var errorMsg = file.ErrorMessage ?? "Không thể tải hóa đơn.";
                    
                    // Kiểm tra các trường hợp lỗi phổ biến
                    if (file.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        errorMsg = "Không tìm thấy đơn hàng hoặc hóa đơn chưa được tạo.";
                    }
                    else if (file.StatusCode == System.Net.HttpStatusCode.BadRequest)
                    {
                        errorMsg = "Đơn hàng chưa được thanh toán. Vui lòng thanh toán trước khi tải hóa đơn.";
                    }
                    else if (file.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        errorMsg = "Bạn chưa đăng nhập hoặc phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";
                    }
                    else if (file.StatusCode == System.Net.HttpStatusCode.InternalServerError)
                    {
                        errorMsg = "Lỗi hệ thống khi tạo hóa đơn. Vui lòng liên hệ quản trị viên.";
                    }

                    TempData["ErrorMessage"] = errorMsg;
                    return RedirectToAction(nameof(Receipt), new { orderId });
                }

                if (file.FileBytes == null || file.FileBytes.Length == 0)
                {
                    TempData["ErrorMessage"] = "File hóa đơn bị trống. Vui lòng thử lại.";
                    return RedirectToAction(nameof(Receipt), new { orderId });
                }

                return File(file.FileBytes, "application/pdf", file.FileName);
            }
            catch (Exception ex)
            {
                // Log exception chi tiết
                TempData["ErrorMessage"] = $"Lỗi khi tải hóa đơn: {ex.Message}";
                return RedirectToAction(nameof(Receipt), new { orderId });
            }
        }

        /// <summary>
        /// POST: Áp dụng ưu đãi / mã giảm giá cho đơn hàng hiện tại
        /// AJAX call từ Promotion Modal → trả về JSON response
        /// </summary>
        [HttpPost("payment/apply-discount")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApplyDiscount(DiscountRequest request)
        {
            if (request == null || request.OrderId <= 0)
            {
                return Json(new { success = false, message = "Dữ liệu ưu đãi không hợp lệ." });
            }

            // Gọi ApiService để áp dụng voucher
            var result = await _paymentApiService.ApplyDiscountAsync(request);
            
            if (result == null)
            {
                return Json(new { success = false, message = "Không thể áp dụng ưu đãi. Vui lòng thử lại sau." });
            }

            if (!result.Success)
            {
                return Json(new { success = false, message = result.Message ?? "Không thể áp dụng mã giảm giá." });
            }

            // ✅ Thành công: trả về JSON để frontend xử lý
            return Json(new { 
                success = true, 
                message = result.Message ?? "Áp dụng mã ưu đãi thành công" 
            });
        }
    }
}

