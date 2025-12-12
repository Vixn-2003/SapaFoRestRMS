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
    /// API Controller cho Owner Warehouse Alert Management
    /// </summary>
    [ApiController]
    [Route("api/owner/warehouse")]
    [Authorize(Roles = "Owner")]
    public class OwnerWarehouseAlertController : ControllerBase
    {
        private readonly IOwnerWarehouseAlertService _warehouseAlertService;
        private readonly ILogger<OwnerWarehouseAlertController> _logger;

        public OwnerWarehouseAlertController(
            IOwnerWarehouseAlertService warehouseAlertService,
            ILogger<OwnerWarehouseAlertController> logger)
        {
            _warehouseAlertService = warehouseAlertService;
            _logger = logger;
        }

        /// <summary>
        /// GET /api/owner/warehouse/alerts
        /// Lấy toàn bộ dữ liệu cảnh báo kho
        /// </summary>
        [HttpGet("alerts")]
        public async Task<ActionResult<WarehouseAlertResponseDto>> GetWarehouseAlerts(CancellationToken ct = default)
        {
            try
            {
                var data = await _warehouseAlertService.GetWarehouseAlertsAsync(ct);
                return Ok(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting warehouse alerts");
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi tải dữ liệu cảnh báo kho." });
            }
        }

        /// <summary>
        /// GET /api/owner/warehouse/summary
        /// Lấy tóm tắt cảnh báo kho
        /// </summary>
        [HttpGet("summary")]
        public async Task<ActionResult<AlertSummaryCardsDto>> GetWarehouseAlertSummary(CancellationToken ct = default)
        {
            try
            {
                var data = await _warehouseAlertService.GetWarehouseAlertsAsync(ct);
                return Ok(data.Summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting warehouse alert summary");
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi tải tóm tắt cảnh báo kho." });
            }
        }
    }
}

