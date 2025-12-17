using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebSapaForestForStaff.Models.Owner;
using WebSapaForestForStaff.Services.Api.Interfaces;

namespace WebSapaForestForStaff.Controllers
{
    /// <summary>
    /// MVC Controller cho Owner Warehouse Alert Management
    /// </summary>
    [Authorize(Policy = "Owner")]
    public class OwnerWarehouseAlertController : Controller
    {
        private readonly IOwnerWarehouseAlertApiService _warehouseAlertApiService;
        private readonly ILogger<OwnerWarehouseAlertController> _logger;

        public OwnerWarehouseAlertController(
            IOwnerWarehouseAlertApiService warehouseAlertApiService,
            ILogger<OwnerWarehouseAlertController> logger)
        {
            _warehouseAlertApiService = warehouseAlertApiService;
            _logger = logger;
        }

        /// <summary>
        /// GET: /OwnerWarehouseAlert/Index
        /// Hiển thị Warehouse Alert View với charts và tables
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var (success, data, message) = await _warehouseAlertApiService.GetWarehouseAlertsAsync();

                if (!success || data == null)
                {
                    _logger.LogWarning("Failed to load warehouse alerts: {Message}", message);
                    return View(new WarehouseAlertViewModel
                    {
                        ErrorMessage = message ?? "Không thể tải dữ liệu cảnh báo kho."
                    });
                }

                var viewModel = new WarehouseAlertViewModel
                {
                    Summary = data.Summary,
                    LowStockItems = data.LowStockItems,
                    NearExpiryItems = data.NearExpiryItems,
                    ExpiredItems = data.ExpiredItems,
                    CategoryDistribution = data.CategoryDistribution,
                    StockLevelChart = data.StockLevelChart,
                    ExpiryTimeline = data.ExpiryTimeline
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading warehouse alerts");
                return View(new WarehouseAlertViewModel
                {
                    ErrorMessage = "Đã xảy ra lỗi khi tải dữ liệu cảnh báo kho."
                });
            }
        }
    }
}

