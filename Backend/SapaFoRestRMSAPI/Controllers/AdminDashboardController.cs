using BusinessAccessLayer.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SapaFoRestRMSAPI.Controllers
{
    /// <summary>
    /// API Controller cho Admin Dashboard
    /// Admin: View comprehensive system dashboard
    /// </summary>
    [ApiController]
    [Route("api/admin/dashboard")]
    [Authorize(Roles = "Admin,Owner")]
    public class AdminDashboardController : ControllerBase
    {
        private readonly IAdminDashboardService _dashboardService;

        public AdminDashboardController(IAdminDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        /// <summary>
        /// GET: /api/admin/dashboard
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
                return StatusCode(500, new
                {
                    success = false,
                    message = "Lỗi khi lấy dữ liệu dashboard",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// GET: /api/admin/dashboard/revenue-7days
        /// Lấy dữ liệu revenue 7 ngày gần nhất
        /// </summary>
        [HttpGet("revenue-7days")]
        public async Task<IActionResult> GetRevenue7Days(CancellationToken ct = default)
        {
            try
            {
                var data = await _dashboardService.GetRevenueLast7DaysAsync(ct);
                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Lỗi khi lấy dữ liệu revenue",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// GET: /api/admin/dashboard/orders-7days
        /// Lấy dữ liệu orders 7 ngày gần nhất
        /// </summary>
        [HttpGet("orders-7days")]
        public async Task<IActionResult> GetOrders7Days(CancellationToken ct = default)
        {
            try
            {
                var data = await _dashboardService.GetOrdersLast7DaysAsync(ct);
                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Lỗi khi lấy dữ liệu orders",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// GET: /api/admin/dashboard/alerts
        /// Lấy tổng hợp cảnh báo kho
        /// </summary>
        [HttpGet("alerts")]
        public async Task<IActionResult> GetAlerts(CancellationToken ct = default)
        {
            try
            {
                var data = await _dashboardService.GetAlertSummaryAsync(ct);
                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Lỗi khi lấy dữ liệu alerts",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// GET: /api/admin/dashboard/logs
        /// Lấy 10 system logs gần nhất
        /// </summary>
        [HttpGet("logs")]
        public async Task<IActionResult> GetRecentLogs(CancellationToken ct = default)
        {
            try
            {
                var data = await _dashboardService.GetRecentLogsAsync(ct);
                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Lỗi khi lấy system logs",
                    error = ex.Message
                });
            }
        }
    }
}

