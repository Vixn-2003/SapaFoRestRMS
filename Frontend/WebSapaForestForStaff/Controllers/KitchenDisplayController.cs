using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using WebSapaForestForStaff.Models.Kitchen;
using WebSapaForestForStaff.Services;

namespace WebSapaFoRestForStaff.Controllers
{
    public class KitchenDisplayController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly KitchenDisplayService _kitchenDisplayService;

        public KitchenDisplayController(
            IConfiguration configuration,
            KitchenDisplayService kitchenDisplayService)
        {
            _configuration = configuration;
            _kitchenDisplayService = kitchenDisplayService;
        }

        /// <summary>
        /// Main KDS screen for Sous Chef
        /// GET: /KitchenDisplay
        /// </summary>
        public IActionResult Index()
        {
            var apiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7096/api";
            var apiBase = apiBaseUrl.Replace("/api", "");
            var signalRHubUrl = $"{apiBase}/kitchenHub";

            // OPTIMIZED: Không load data ở server-side, để client-side load để tránh double loading
            // Chỉ truyền config cần thiết
            var viewModel = new KitchenDisplayViewModel
            {
                ActiveOrders = new(), // Empty list - sẽ load từ client
                CourseTypes = new(), // Empty list - sẽ load từ client nếu cần
                ApiBaseUrl = apiBaseUrl,
                SignalRHubUrl = signalRHubUrl
            };

            return View(viewModel);
        }

        /// <summary>
        /// Station screen (filtered by category name)
        /// GET: /KitchenDisplay/Station?categoryName=Xào
        /// </summary>
        public IActionResult Station(string categoryName)
        {
            var apiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7096/api";
            var apiBase = apiBaseUrl.Replace("/api", "");
            var signalRHubUrl = $"{apiBase}/kitchenHub";

            // Set ViewBag for View compatibility
            ViewBag.CategoryName = categoryName ?? "";
            ViewBag.ApiBaseUrl = apiBaseUrl;
            ViewBag.SignalRHubUrl = signalRHubUrl;

            // OPTIMIZED: Không load data ở server-side, để client-side load
            var viewModel = new KitchenStationViewModel
            {
                CategoryName = categoryName ?? "",
                StationItems = null, // Sẽ load từ client
                ApiBaseUrl = apiBaseUrl,
                SignalRHubUrl = signalRHubUrl
            };

            return View(viewModel);
        }
    }
}