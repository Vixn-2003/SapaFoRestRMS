using Microsoft.AspNetCore.Mvc;
using WebSapaFoRestForCustomer.Models;

namespace WebSapaFoRestForCustomer.Controllers
{
    public class LogoController : Controller
    {
        private readonly HttpClient _httpClient;

        public LogoController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.BaseAddress = new Uri("https://localhost:7096"); // API backend
        }

        [HttpGet]
        public async Task<IActionResult> Active()
        {
            var logos = await _httpClient.GetFromJsonAsync<List<SystemLogoViewDto>>("/api/SystemLogo/active");

            if (logos != null && logos.Count > 0)
            {
                return Json(new { logoUrl = logos[0].LogoUrl });
            }

            return Json(new { logoUrl = "/images/logo.png" }); // fallback
        }
    }
}
