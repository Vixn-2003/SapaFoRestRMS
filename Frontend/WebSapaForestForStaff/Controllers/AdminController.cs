using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebSapaForestForStaff.Models.Admin;
using WebSapaForestForStaff.Services;
using WebSapaForestForStaff.Services.Api.Interfaces;
using WebSapaForestForStaff.DTOs.AdminDashboard;

namespace WebSapaForestForStaff.Controllers
{
    [Authorize(Roles = "Admin,Owner")]
    public class AdminController : Controller
    {
        private readonly ApiService _apiService;
        private readonly IAdminDashboardApiService _adminDashboardApiService;

        public AdminController(ApiService apiService, IAdminDashboardApiService adminDashboardApiService)
        {
            _apiService = apiService;
            _adminDashboardApiService = adminDashboardApiService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // Lấy dữ liệu từ Admin Dashboard API mới
            var dashboard = await _adminDashboardApiService.GetDashboardAsync();

            if (dashboard == null)
            {
                // Fallback: Nếu API mới không hoạt động, dùng API cũ
                var vm = new AdminDashboardViewModel
                {
                    TotalUsers = await _apiService.GetTotalUsersAsync(),
                    TotalReservationsPendingOrConfirmed = await _apiService.GetTotalPendingOrConfirmedReservationsAsync(),
                    TotalEvents = await _apiService.GetTotalEventsAsync(),
                    ErrorMessage = "Không thể tải dữ liệu dashboard đầy đủ. Hiển thị dữ liệu cơ bản.",
                    Dashboard = new AdminDashboardDto() // Empty dashboard
                };
                return View(vm);
            }

            // Trả về ViewModel với dashboard data đầy đủ
            var viewModel = new AdminDashboardViewModel
            {
                Dashboard = dashboard,
                // Legacy properties (tính từ dashboard data)
                TotalUsers = dashboard.KpiCards.TotalUsers,
                TotalReservationsPendingOrConfirmed = dashboard.KpiCards.TodayReservations,
                TotalEvents = 0 // Có thể thêm nếu cần
            };

            return View(viewModel);
        }
    }
}


