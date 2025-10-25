using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebSapaForestForStaff.Models.Admin;
using WebSapaForestForStaff.Services;

namespace WebSapaForestForStaff.Controllers
{
    [Authorize(Roles = "Admin,Owner")]
    public class AdminController : Controller
    {
        private readonly ApiService _apiService;

        public AdminController(ApiService apiService)
        {
            _apiService = apiService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var vm = new AdminDashboardViewModel
            {
                TotalUsers = await _apiService.GetTotalUsersAsync(),
                TotalReservationsPendingOrConfirmed = await _apiService.GetTotalPendingOrConfirmedReservationsAsync(),
                TotalEvents = await _apiService.GetTotalEventsAsync()
            };
            return View(vm);
        }
    }
}


