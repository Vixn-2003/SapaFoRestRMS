using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using WebSapaForestForStaff.DTOs.Waiter;

namespace WebSapaForestForStaff.Controllers
{
    public class WaiterOrderTrackingController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public WaiterOrderTrackingController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IActionResult> Index()
        {
            var httpClient = _httpClientFactory.CreateClient("BackendApi");
            var apiUrl = "https://localhost:7096/api/WaiterOrderTracking";

            WaiterOrderTrackingDto model = new WaiterOrderTrackingDto();

            try
            {
                var response = await httpClient.GetAsync(apiUrl);
                if (response.IsSuccessStatusCode)
                {
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    var result = await response.Content.ReadFromJsonAsync<JsonElement>(options);
                    if (result.TryGetProperty("data", out var dataElement))
                    {
                        model = JsonSerializer.Deserialize<WaiterOrderTrackingDto>(dataElement.GetRawText(), options) ?? new WaiterOrderTrackingDto();
                    }
                }
                else
                {
                    ViewData["ErrorMessage"] = "Không thể tải dữ liệu theo dõi đơn hàng.";
                }
            }
            catch (Exception ex)
            {
                ViewData["ErrorMessage"] = $"Lỗi kết nối: {ex.Message}";
            }

            ViewBag.ApiBaseUrl = "https://localhost:7096/api";
            return View(model);
        }
    }
}

