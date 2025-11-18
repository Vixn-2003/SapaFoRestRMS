using Microsoft.AspNetCore.Mvc;

namespace WebSapaForestForStaff.Controllers
{
    public class PaymentController : Controller
    {
        // API Base URL - có thể lấy từ appsettings
        private readonly string _apiBaseUrl = "https://localhost:7000/api";

        public IActionResult DepositPayment(int id, decimal amount)
        {
            // Có thể truy xuất thêm thông tin khách hàng và đặt bàn
            ViewBag.ReservationId = id;
            ViewBag.Amount = amount;
            return View();
        }

        // 1. Màn hình danh sách đơn chờ thanh toán
        public IActionResult OrderSelection()
        {
            ViewBag.ApiBaseUrl = _apiBaseUrl;
            return View();
        }

        // 2. Màn hình chi tiết đơn hàng
        public IActionResult OrderDetail(int id)
        {
            ViewBag.OrderId = id;
            ViewBag.ApiBaseUrl = _apiBaseUrl;
            return View();
        }

        // 3. Màn hình chọn phương thức thanh toán
        public IActionResult PaymentMethod(int orderId)
        {
            ViewBag.OrderId = orderId;
            ViewBag.ApiBaseUrl = _apiBaseUrl;
            return View();
        }

        // 4. Màn hình xác nhận thanh toán
        public IActionResult PaymentConfirmation(int orderId, string method)
        {
            ViewBag.OrderId = orderId;
            ViewBag.PaymentMethod = method;
            ViewBag.ApiBaseUrl = _apiBaseUrl;
            return View();
        }

        // 5. Màn hình kết quả thanh toán
        public IActionResult PaymentResult(int orderId, string? sessionId = null)
        {
            ViewBag.OrderId = orderId;
            ViewBag.SessionId = sessionId;
            ViewBag.ApiBaseUrl = _apiBaseUrl;
            return View();
        }

        // 6. Màn hình in/gửi hóa đơn
        public IActionResult Receipt(int orderId)
        {
            ViewBag.OrderId = orderId;
            ViewBag.ApiBaseUrl = _apiBaseUrl;
            return View();
        }
    }
}
