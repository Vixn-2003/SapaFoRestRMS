using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using WebSapaFoRestForCustomer.Models;

namespace WebSapaFoRestForCustomer.Controllers
{
    [Authorize(Roles = "Customer")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly HttpClient _httpClient;
        public HomeController(ILogger<HomeController> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.BaseAddress = new Uri("https://localhost:7096/");
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                // API 
                var bestSellers = await _httpClient.GetFromJsonAsync<List<MenuItemDto>>("api/MenuItem/top-best-sellers");
                var combos = await _httpClient.GetFromJsonAsync<List<ComboDto>>("api/Combos");
                var events = await _httpClient.GetFromJsonAsync<List<EventDto>>("api/Events/top6");
                
                //  sang View
                ViewBag.BestSellers = bestSellers ?? new List<MenuItemDto>();
                ViewBag.Combos = combos ?? new List<ComboDto>();
                ViewBag.Events = events ?? new List<EventDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "L?i khi g?i API trong HomeController");
                ViewBag.BestSellers = new List<MenuItemDto>();
                ViewBag.Combos = new List<ComboDto>();
            }

            return View();

        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
