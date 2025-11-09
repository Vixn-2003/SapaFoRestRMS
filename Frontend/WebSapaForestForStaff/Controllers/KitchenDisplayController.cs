using Microsoft.AspNetCore.Mvc;

namespace WebSapaFoRestForStaff.Controllers
{
    public class KitchenDisplayController : Controller
    {
        private readonly IConfiguration _configuration;

        public KitchenDisplayController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Main KDS screen for Sous Chef
        /// GET: /KitchenDisplay
        /// </summary>
        public IActionResult Index()
        {
            ViewBag.ApiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7096/api";
            var apiBase = ViewBag.ApiBaseUrl.ToString().Replace("/api", "");
            ViewBag.SignalRHubUrl = $"{apiBase}/kitchenHub";
            return View();
        }

        /// <summary>
        /// Station screen (filtered by category name)
        /// GET: /KitchenDisplay/Station?categoryName=Xào
        /// </summary>
        public IActionResult Station(string categoryName)
        {
            ViewBag.CategoryName = categoryName;
            ViewBag.ApiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7096/api";
            var apiBase = ViewBag.ApiBaseUrl.ToString().Replace("/api", "");
            ViewBag.SignalRHubUrl = $"{apiBase}/kitchenHub";
            return View();
        }
    }
}