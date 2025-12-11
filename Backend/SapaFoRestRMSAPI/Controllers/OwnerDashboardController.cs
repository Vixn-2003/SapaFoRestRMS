using BusinessAccessLayer.DTOs.Owner;
using BusinessAccessLayer.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SapaFoRestRMSAPI.Controllers
{
    /// <summary>
    /// API Controller cho Owner Dashboard
    /// </summary>
    [ApiController]
    [Route("api/owner/dashboard")]
    [Authorize(Roles = "Owner")]
    public class OwnerDashboardController : ControllerBase
    {
        private readonly IOwnerDashboardService _dashboardService;
        private readonly ILogger<OwnerDashboardController> _logger;

        public OwnerDashboardController(
            IOwnerDashboardService dashboardService,
            ILogger<OwnerDashboardController> logger)
        {
            _dashboardService = dashboardService;
            _logger = logger;
        }

        /// <summary>
        /// GET /api/owner/dashboard
        /// Lấy toàn bộ dữ liệu dashboard cho Owner
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<OwnerDashboardDto>> GetDashboard(CancellationToken ct = default)
        {
            try
            {
                var data = await _dashboardService.GetDashboardDataAsync(ct);
                return Ok(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting owner dashboard data");
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi tải dữ liệu dashboard." });
            }
        }
    }
}

