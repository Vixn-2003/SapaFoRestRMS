using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;
using WebSapaFoRestForCustomer.Models;
using System.Net.Http;
using System.Threading.Tasks;

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(ReservationViewModel model)
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
                Notes = model.Notes
            };

            var jsonContent = JsonConvert.SerializeObject(dto);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _client.PostAsync(_apiUrl, content);

            if (response.IsSuccessStatusCode)
            {
                // Trả về JSON cho AJAX
                return Json(new { success = true });
            }
            else
            {
                ModelState.AddModelError("", "Không thể đặt bàn. Vui lòng thử lại.");
                return PartialView("_ReservationForm", model);
            }
        }


    }
}
