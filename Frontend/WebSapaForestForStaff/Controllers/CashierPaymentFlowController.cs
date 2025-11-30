using Microsoft.AspNetCore.Mvc;
using WebSapaForestForStaff.DTOs.Payment;
using WebSapaForestForStaff.Services.Api.Interfaces;
using WebSapaForestForStaff.ViewModels.Payment;

namespace WebSapaForestForStaff.Controllers
{
    [Route("cashier-flow")]
    public class CashierPaymentFlowController : Controller
    {
        private readonly IPaymentApiService _paymentApiService;

        public CashierPaymentFlowController(IPaymentApiService paymentApiService)
        {
            _paymentApiService = paymentApiService;
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

        [HttpGet("orders/{id}")]
        public async Task<IActionResult> OrderDetail(int id)
        {
            var order = await _paymentApiService.GetOrderDetailAsync(id);
            if (order == null) return NotFound();
            return View("~/Views/CashierFlow/ConfirmOrder.cshtml", order);
        }

        [HttpGet("confirm/{orderId}")]
        public async Task<IActionResult> CustomerConfirm(int orderId)
        {
            var order = await _paymentApiService.GetOrderDetailAsync(orderId);
            if (order == null) return NotFound();
            return View("~/Views/CashierFlow/ConfirmOrder.cshtml", order);
        }

        [HttpPost("confirm")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmOrder(ConfirmOrderRequest request)
        {
            if (request.Items == null || request.Items.Count == 0)
            {
                TempData["ErrorMessage"] = "Vui lòng nhập số lượng món khách đã dùng.";
                return RedirectToAction(nameof(CustomerConfirm), new { orderId = request.OrderId });
            }

            var result = await _paymentApiService.ConfirmCustomerOrderAsync(request);
            if (!result.Success)
            {
                TempData["ErrorMessage"] = result.Message;
                return RedirectToAction(nameof(CustomerConfirm), new { orderId = request.OrderId });
            }

            TempData["SuccessMessage"] = "Khách đã xác nhận đơn hàng thành công. Bạn có thể tiếp tục xử lý thanh toán.";
            return RedirectToAction(nameof(OrderDetail), new { id = request.OrderId });
        }

        [HttpGet("payment/{id}")]
        public async Task<IActionResult> Payment(int id)
        {
            var order = await _paymentApiService.GetOrderDetailAsync(id);
            if (order == null) return NotFound();
            
            // Kiểm tra xem đơn hàng đã được khách xác nhận chưa
            var isConfirmed = !string.IsNullOrEmpty(order.Status) && 
                              (order.Status.Equals("confirmed", StringComparison.OrdinalIgnoreCase) ||
                               order.Status.Equals("pending-payment", StringComparison.OrdinalIgnoreCase) ||
                               order.Status.Equals("paid", StringComparison.OrdinalIgnoreCase) ||
                               order.Status.Equals("completed", StringComparison.OrdinalIgnoreCase));
            
            if (!isConfirmed)
            {
                TempData["ErrorMessage"] = "Đơn hàng chưa được khách xác nhận. Vui lòng yêu cầu khách xác nhận số lượng món đã dùng trước khi thanh toán.";
                return RedirectToAction(nameof(CustomerConfirm), new { orderId = id });
            }
            
            return View("~/Views/CashierFlow/Payment.cshtml", order);
        }

        [HttpPost("payment/initiate")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> InitiatePayment(PaymentInitiateRequest request)
        {
            if (request.OrderId <= 0 || string.IsNullOrEmpty(request.Method))
            {
                TempData["ErrorMessage"] = "Vui lòng chọn phương thức thanh toán.";
                return RedirectToAction(nameof(Payment), new { id = request.OrderId });
            }

            // Kiểm tra xem đơn hàng đã được khách xác nhận chưa
            var order = await _paymentApiService.GetOrderDetailAsync(request.OrderId);
            if (order == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn hàng.";
                return RedirectToAction(nameof(OrderSelection));
            }
            
            var isConfirmed = !string.IsNullOrEmpty(order.Status) && 
                              (order.Status.Equals("confirmed", StringComparison.OrdinalIgnoreCase) ||
                               order.Status.Equals("pending-payment", StringComparison.OrdinalIgnoreCase) ||
                               order.Status.Equals("paid", StringComparison.OrdinalIgnoreCase) ||
                               order.Status.Equals("completed", StringComparison.OrdinalIgnoreCase));
            
            if (!isConfirmed)
            {
                TempData["ErrorMessage"] = "Đơn hàng chưa được khách xác nhận. Vui lòng yêu cầu khách xác nhận số lượng món đã dùng trước khi thanh toán.";
                return RedirectToAction(nameof(CustomerConfirm), new { orderId = request.OrderId });
            }

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
    }
}

