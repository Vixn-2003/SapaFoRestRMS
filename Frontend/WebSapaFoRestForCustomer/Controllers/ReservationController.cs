using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
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
        public IActionResult ReservationList()
        {
            return View();
        }
        // Gửi OTP
        [HttpPost]
        public async Task<IActionResult> SendOtp([FromBody] OtpRequestDto dto)
        {
            if (string.IsNullOrEmpty(dto.Phone))
                return BadRequest(new { message = "Số điện thoại không hợp lệ." });

            // Gửi đúng dạng string theo API
            var jsonContent = JsonConvert.SerializeObject(dto.Phone);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _client.PostAsync($"{_apiUrl}/send-otp", content);
            var json = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
                return Ok(new { success = true, message = "OTP đã gửi về điện thoại." });
            else
                return BadRequest(new { success = false, message = json });
        }

        // Xác nhận & tạo đặt bàn
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm(ReservationViewModel model)
        {
            if (!ModelState.IsValid)
                return PartialView("_ReservationForm", model);

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
            var json = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
                return Json(new { success = true, message = "Đặt bàn thành công" });
            else
                return Json(new { success = false, message = json });
        }
    }
}
