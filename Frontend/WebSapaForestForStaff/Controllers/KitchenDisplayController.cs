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
        public async Task<IActionResult> Index()
        {
            var apiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7096/api";
            var apiBase = apiBaseUrl.Replace("/api", "");
            var signalRHubUrl = $"{apiBase}/kitchenHub";

            // Load initial data from Service
            var activeOrders = await _kitchenDisplayService.GetActiveOrdersAsync() ?? new();
            var courseTypes = await _kitchenDisplayService.GetCourseTypesAsync() ?? new();

            var viewModel = new KitchenDisplayViewModel
            {
                ActiveOrders = activeOrders,
                CourseTypes = courseTypes,
                ApiBaseUrl = apiBaseUrl,
                SignalRHubUrl = signalRHubUrl
            };

            return View(viewModel);
        }

        /// <summary>
        /// Station screen (filtered by category name)
        /// GET: /KitchenDisplay/Station?categoryName=Xào
        /// </summary>
        public async Task<IActionResult> Station(string categoryName)
        {
            var apiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7096/api";
            var apiBase = apiBaseUrl.Replace("/api", "");
            var signalRHubUrl = $"{apiBase}/kitchenHub";

            // Set ViewBag for View compatibility
            ViewBag.CategoryName = categoryName ?? "";
            ViewBag.ApiBaseUrl = apiBaseUrl;
            ViewBag.SignalRHubUrl = signalRHubUrl;

            // Load station items from Service
            var stationItems = await _kitchenDisplayService.GetStationItemsByCategoryAsync(categoryName ?? "");

            var viewModel = new KitchenStationViewModel
            {
                CategoryName = categoryName ?? "",
                StationItems = stationItems,
                ApiBaseUrl = apiBaseUrl,
                SignalRHubUrl = signalRHubUrl
            };

            return View(viewModel);
        }
    }
}