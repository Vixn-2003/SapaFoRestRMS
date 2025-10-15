using Microsoft.AspNetCore.Mvc;

namespace WebSapaForestForStaff.Controllers
{
    public class PaymentController : Controller
    {
        public IActionResult DepositPayment(int id, decimal amount)
        {
            // Có thể truy xuất thêm thông tin khách hàng và đặt bàn
            ViewBag.ReservationId = id;
            ViewBag.Amount = amount;
            return View();
        }
    }

}
