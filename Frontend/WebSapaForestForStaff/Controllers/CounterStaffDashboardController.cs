using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using WebSapaForestForStaff.Services.Api.Interfaces;
using WebSapaForestForStaff.ViewModels.CounterStaff;

namespace WebSapaForestForStaff.Controllers
{
    /// <summary>
    /// MVC Controller cho Counter Staff Dashboard - UC122
    /// </summary>
    [Route("counter-staff/dashboard")]
    public class CounterStaffDashboardController : Controller
    {
        private readonly ICounterStaffDashboardApiService _dashboardApiService;

        public CounterStaffDashboardController(ICounterStaffDashboardApiService dashboardApiService)
        {
            _dashboardApiService = dashboardApiService;
        }

        /// <summary>
        /// GET: /counter-staff/dashboard
        /// Hiển thị Counter Staff Dashboard
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var dashboard = await _dashboardApiService.GetDashboardAsync();
            if (dashboard == null)
            {
                TempData["ErrorMessage"] = "Không thể tải dữ liệu dashboard.";
                dashboard = new DTOs.CounterStaff.CounterStaffDashboardDto();
            }

            var viewModel = new CounterStaffDashboardViewModel
            {
                Dashboard = dashboard
            };

            return View("~/Views/CounterStaffDashboard/Index.cshtml", viewModel);
        }
    }
}

