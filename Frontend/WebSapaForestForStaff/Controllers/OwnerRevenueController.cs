using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebSapaForestForStaff.DTOs.Owner;
using WebSapaForestForStaff.Models.Owner;
using WebSapaForestForStaff.Services.Api.Interfaces;

namespace WebSapaForestForStaff.Controllers
{
    /// <summary>
    /// MVC Controller cho Owner Revenue Management
    /// </summary>
    [Authorize(Policy = "Owner")]
    public class OwnerRevenueController : Controller
    {
        private readonly IOwnerRevenueApiService _revenueApiService;
        private readonly ILogger<OwnerRevenueController> _logger;

        public OwnerRevenueController(
            IOwnerRevenueApiService revenueApiService,
            ILogger<OwnerRevenueController> logger)
        {
            _revenueApiService = revenueApiService;
            _logger = logger;
        }

        /// <summary>
        /// GET: /OwnerRevenue/Index
        /// Hiển thị Revenue View với charts và filters
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index(
            DateTime? startDate = null,
            DateTime? endDate = null,
            string? branch = "ALL",
            string? paymentMethod = "ALL")
        {
            try
            {
                // Set default dates if not provided
                endDate ??= DateTime.Today;
                startDate ??= endDate.Value.AddDays(-30);

                var request = new RevenueFilterRequestDto
                {
                    StartDate = startDate,
                    EndDate = endDate,
                    BranchName = branch,
                    PaymentMethod = paymentMethod
                };

                var (success, data, message) = await _revenueApiService.GetRevenueDataAsync(request);

                if (!success || data == null)
                {
                    _logger.LogWarning("Failed to load revenue data: {Message}", message);
                    return View(new RevenueViewModel
                    {
                        ErrorMessage = message ?? "Không thể tải dữ liệu doanh thu.",
                        StartDate = startDate,
                        EndDate = endDate,
                        SelectedBranch = branch,
                        SelectedPaymentMethod = paymentMethod
                    });
                }

                var viewModel = new RevenueViewModel
                {
                    Summary = data.Summary,
                    Details = data.Details,
                    TrendData = data.TrendData,
                    PaymentBreakdown = data.PaymentBreakdown,
                    BranchComparison = data.BranchComparison,
                    StartDate = startDate,
                    EndDate = endDate,
                    SelectedBranch = branch,
                    SelectedPaymentMethod = paymentMethod
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading revenue data");
                return View(new RevenueViewModel
                {
                    ErrorMessage = "Đã xảy ra lỗi khi tải dữ liệu doanh thu.",
                    StartDate = startDate,
                    EndDate = endDate,
                    SelectedBranch = branch,
                    SelectedPaymentMethod = paymentMethod
                });
            }
        }
    }
}

