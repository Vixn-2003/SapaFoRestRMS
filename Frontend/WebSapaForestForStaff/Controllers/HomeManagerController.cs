using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http;
using WebSapaForestForStaff.DTOs;

namespace WebSapaForestForStaff.Controllers
{
    public class HomeManagerController : Controller
    {
        private readonly HttpClient _client;

        public HomeManagerController(IHttpClientFactory clientFactory)
        {
            _client = clientFactory.CreateClient();
            _client.BaseAddress = new Uri("https://localhost:7096/api/");
        }

        public async Task<IActionResult> Index()
        {
            int pendingCount = 0;

            try
            {
                var response = await _client.GetAsync("ReservationStaff/reservations/pending-count");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<PendingCountResponse>(json);

                    if (result != null)
                        pendingCount = result.PendingCount;
                }
            }
            catch
            {
                pendingCount = 0;
            }

            ViewBag.PendingCount = pendingCount;
            return View();
        }
        public class PendingCountResponse
        {
            public int PendingCount { get; set; }
        }
    }
}
