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
    /// API Controller cho Owner Revenue Management
    /// </summary>
    [ApiController]
    [Route("api/owner/revenue")]
    [Authorize(Roles = "Owner")]
    public class OwnerRevenueController : ControllerBase
    {
        private readonly IOwnerRevenueService _revenueService;
        private readonly ILogger<OwnerRevenueController> _logger;

        public OwnerRevenueController(
            IOwnerRevenueService revenueService,
            ILogger<OwnerRevenueController> logger)
        {
            _revenueService = revenueService;
            _logger = logger;
        }

        /// <summary>
        /// POST /api/owner/revenue/filter
        /// Lấy dữ liệu revenue theo filter
        /// </summary>
        [HttpPost("filter")]
        public async Task<ActionResult<RevenueResponseDto>> GetRevenueData(
            [FromBody] RevenueFilterRequestDto request,
            CancellationToken ct = default)
        {
            try
            {
                var data = await _revenueService.GetRevenueDataAsync(request, ct);
                return Ok(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting revenue data with filter: {@Filter}", request);
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi tải dữ liệu doanh thu." });
            }
        }

        /// <summary>
        /// GET /api/owner/revenue/summary
        /// Lấy tóm tắt doanh thu (30 ngày gần nhất)
        /// </summary>
        [HttpGet("summary")]
        public async Task<ActionResult<RevenueResponseDto>> GetRevenueSummary(CancellationToken ct = default)
        {
            try
            {
                var request = new RevenueFilterRequestDto
                {
                    StartDate = DateTime.Today.AddDays(-30),
                    EndDate = DateTime.Today,
                    PaymentMethod = "ALL",
                    BranchName = "ALL"
                };

                var data = await _revenueService.GetRevenueDataAsync(request, ct);
                return Ok(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting revenue summary");
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi tải tóm tắt doanh thu." });
            }
        }
    }
}

