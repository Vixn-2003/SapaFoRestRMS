using Microsoft.AspNetCore.Mvc;
using WebSapaFoRestForCustomer.Models;

namespace WebSapaFoRestForCustomer.Controllers
{
    public class BannerController : Controller
    {
        private readonly HttpClient _httpClient;

        public BannerController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.BaseAddress = new Uri("https://localhost:7096");
        }

        [HttpGet]
        public async Task<IActionResult> Active()
        {
            var banners = await _httpClient.GetFromJsonAsync<List<BrandBannerViewDto>>("/api/BrandBanner/active");
            return Json(banners ?? new List<BrandBannerViewDto>());
        }
    }
}
