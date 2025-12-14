using BusinessAccessLayer.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SapaFoRestRMSAPI.Controllers
{
    /// <summary>
    /// API Controller cho Counter Staff Dashboard - UC122
    /// Counter Staff: View dashboard overview
    /// </summary>
    [ApiController]
    [Route("api/counter/dashboard")]
    [Authorize(Roles = "Staff")]
    public class CounterStaffDashboardController : ControllerBase
    {
        private readonly ICounterStaffDashboardService _dashboardService;

        public CounterStaffDashboardController(ICounterStaffDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        /// <summary>
        /// GET: /api/counter/dashboard
        /// Lấy toàn bộ dữ liệu dashboard
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetDashboard(CancellationToken ct = default)
        {
            try
            {
                var dashboard = await _dashboardService.GetDashboardDataAsync(ct);
                return Ok(dashboard);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy dữ liệu dashboard", error = ex.Message });
            }
        }
    }
}

