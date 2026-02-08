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

       
    }
}