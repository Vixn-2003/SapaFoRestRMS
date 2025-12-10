using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Security.Claims;
using System.Text;
using WebSapaFoRestForCustomer.Models;

namespace WebSapaFoRestForCustomer.Controllers
{
    public class ReservationController : Controller
    {
        private readonly HttpClient _client;
        private readonly string _apiUrl = "https://localhost:7096/api/Reservation";

        public ReservationController(IHttpClientFactory httpClientFactory)
        {
            _client = httpClientFactory.CreateClient();
        }

        [Authorize(Roles = "Customer")]
        public IActionResult ReservationList()
        {
            // Lấy customerId từ claim
            var customerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(customerIdClaim))
                return RedirectToAction("Login", "Auth"); // chưa login thì chuyển tới login

            ViewBag.CustomerId = customerIdClaim; // truyền xuống view
            Console.WriteLine(ViewBag.CustomerId);
            return View();
        }

        // Gửi OTP
        [HttpPost]
        public async Task<IActionResult> SendOtp([FromBody] OtpRequestDto dto)
        {
            if (string.IsNullOrEmpty(dto.Phone))
                return BadRequest(new { success = false, message = "Số điện thoại không hợp lệ." });

            // Gửi đúng dạng string theo API
            var jsonContent = JsonConvert.SerializeObject(dto.Phone);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _client.PostAsync($"{_apiUrl}/send-otp", content);
            var json = await response.Content.ReadAsStringAsync();

            try
            {
                dynamic? apiResult = JsonConvert.DeserializeObject(json);
                string message = apiResult?.message ?? (response.IsSuccessStatusCode
                    ? "OTP đã được gửi."
                    : "Không thể gửi OTP, vui lòng thử lại.");

                if (response.IsSuccessStatusCode)
                {
                    return Ok(new { success = true, message });
                }
                else
                {
                    return BadRequest(new { success = false, message });
                }
            }
            catch
            {
                if (response.IsSuccessStatusCode)
                    return Ok(new { success = true, message = "OTP đã được gửi." });
                else
                    return BadRequest(new { success = false, message = json });
            }
        }

        // Xác nhận & tạo đặt bàn
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm(ReservationViewModel model)
        {
            // ⚠️ Không trả BadRequest nữa để JS không nhảy vào error()
            if (!ModelState.IsValid)
            {
                var firstError = ModelState.Values
                                           .SelectMany(v => v.Errors)
                                           .FirstOrDefault()?.ErrorMessage
                                ?? "Dữ liệu không hợp lệ.";

                return Ok(new { success = false, message = firstError });
            }

            var dto = new
            {
                CustomerName = model.CustomerName,
                Phone = model.Phone,
                ReservationDate = model.ReservationDate,
                ReservationTime = model.ReservationTime,
                NumberOfGuests = model.NumberOfGuests,
                Notes = model.Notes,
                OtpCode = model.OtpCode
            };

            var jsonContent = JsonConvert.SerializeObject(dto);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _client.PostAsync($"{_apiUrl}/confirm", content);
            var apiJson = await response.Content.ReadAsStringAsync();

            // API /api/Reservation/confirm trả JSON:
            // { success, message, orderId, requiredDeposit, payUrl, timeSlot, ... }
            // → forward nguyên cho JS ở view
            return Content(apiJson, "application/json");
        }
        [HttpGet]
        public IActionResult PaymentResult(
           string orderId,
           int resultCode,
           long amount,
           long? transId,
           string message)
        {
            if (resultCode == 0)
            {
                // Thanh toán thành công
                TempData["ReservationSuccess"] =
                    "Đặt bàn thành công! Cảm ơn bạn đã thanh toán tiền cọc.";
            }
            else
            {
                // Thanh toán thất bại
                TempData["ReservationError"] =
                    $"Thanh toán không thành công (resultCode: {resultCode}).";
            }

            // Chuyển về Home/Index
            return RedirectToAction("Index", "Home");
        }
    }
}
