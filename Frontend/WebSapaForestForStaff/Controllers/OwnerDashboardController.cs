using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebSapaForestForStaff.Models.Owner;
using WebSapaForestForStaff.Services.Api.Interfaces;

namespace WebSapaForestForStaff.Controllers
{
    /// <summary>
    /// MVC Controller cho Owner Dashboard
    /// </summary>
    [Authorize(Policy = "Owner")]
    public class OwnerDashboardController : Controller
    {
        private readonly IOwnerDashboardApiService _dashboardApiService;
        private readonly ILogger<OwnerDashboardController> _logger;

        public OwnerDashboardController(
            IOwnerDashboardApiService dashboardApiService,
            ILogger<OwnerDashboardController> logger)
        {
            _dashboardApiService = dashboardApiService;
            _logger = logger;
        }

        /// <summary>
        /// GET: /OwnerDashboard/Index
        /// Hiển thị Owner Dashboard với charts
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var (success, data, message) = await _dashboardApiService.GetDashboardDataAsync();

                if (!success || data == null)
                {
                    _logger.LogWarning("Failed to load dashboard data: {Message}", message);
                    return View(new OwnerDashboardViewModel
                    {
                        ErrorMessage = message ?? "Không thể tải dữ liệu dashboard."
                    });
                }

                var viewModel = new OwnerDashboardViewModel
                {
                    KpiCards = data.KpiCards,
                    RevenueTrend = data.RevenueTrend,
                    TopSellingItems = data.TopSellingItems,
                    BranchComparison = data.BranchComparison,
                    AlertsSummary = data.AlertsSummary
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading owner dashboard");
                return View(new OwnerDashboardViewModel
                {
                    ErrorMessage = "Đã xảy ra lỗi khi tải dashboard."
                });
            }
        }
    }
}

