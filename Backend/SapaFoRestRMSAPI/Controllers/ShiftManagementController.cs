using BusinessAccessLayer.DTOs.ShiftManagement;
using BusinessAccessLayer.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading;
using System.Threading.Tasks;



namespace SapaFoRestRMSAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Require authentication
public class ShiftManagementController : ControllerBase
{
    private readonly IShiftManagementService _shiftManagementService;
    private readonly ILogger<ShiftManagementController> _logger;

    public ShiftManagementController(
        IShiftManagementService shiftManagementService,
        ILogger<ShiftManagementController> logger)
    {
        _shiftManagementService = shiftManagementService;
        _logger = logger;
    }

    // ========== OPENING SHIFT ==========

    /// <summary>
    /// UC125 - Declare Opening Balance
    /// POST /api/ShiftManagement/opening/declare
    /// </summary>
    [HttpPost("opening/declare")]
    public async Task<IActionResult> DeclareOpeningBalance(
        [FromBody] ShiftOpeningDeclareRequestDto request,
        CancellationToken ct = default)
    {
        try
        {
            var result = await _shiftManagementService.DeclareOpeningBalanceAsync(request, ct);

            if (!result.Success)
            {
                return BadRequest(new { message = result.Message });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error declaring opening balance for staff {StaffId}", request.StaffId);
            return StatusCode(500, new { message = "Lỗi hệ thống khi khai báo số dư đầu ca." });
        }
    }

    /// <summary>
    /// UC126 - Submit Opening Denominations
    /// POST /api/ShiftManagement/opening/denominations
    /// </summary>
    [HttpPost("opening/denominations")]
    public async Task<IActionResult> SubmitOpeningDenominations(
        [FromBody] ShiftOpeningDenominationsRequestDto request,
        CancellationToken ct = default)
    {
        try
        {
            var result = await _shiftManagementService.SubmitOpeningDenominationsAsync(request, ct);

            if (!result.Success)
            {
                return BadRequest(new { message = result.Message });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting opening denominations for shift {ShiftId}", request.ShiftId);
            return StatusCode(500, new { message = "Lỗi hệ thống khi nhập mệnh giá tiền." });
        }
    }

    /// <summary>
    /// UC127 - Confirm Shift Opening
    /// POST /api/ShiftManagement/opening/confirm
    /// </summary>
    [HttpPost("opening/confirm")]
    public async Task<IActionResult> ConfirmShiftOpening(
        [FromBody] ShiftOpeningConfirmRequestDto request,
        CancellationToken ct = default)
    {
        try
        {
            var result = await _shiftManagementService.ConfirmShiftOpeningAsync(request, ct);

            if (!result.Success)
            {
                return BadRequest(new { message = result.Message });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming shift opening for shift {ShiftId}", request.ShiftId);
            return StatusCode(500, new { message = "Lỗi hệ thống khi xác nhận mở ca." });
        }
    }

    // ========== CLOSING SHIFT ==========

    /// <summary>
    /// UC128 - Count Closing Cash
    /// POST /api/ShiftManagement/closing/denominations
    /// </summary>
    [HttpPost("closing/denominations")]
    public async Task<IActionResult> CountClosingCash(
        [FromBody] ShiftDenominationsRequestDto request,
        CancellationToken ct = default)
    {
        try
        {
            var result = await _shiftManagementService.CountClosingCashAsync(request, ct);

            if (!result.Success)
            {
                return BadRequest(new { message = result.Message });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error counting closing cash for shift {ShiftId}", request.ShiftId);
            return StatusCode(500, new { message = "Lỗi hệ thống khi kiểm kê tiền cuối ca." });
        }
    }

    /// <summary>
    /// UC129 - Calculate Difference
    /// POST /api/ShiftManagement/closing/calculate
    /// </summary>
    [HttpPost("closing/calculate")]
    public async Task<IActionResult> CalculateDifference(
        [FromBody] CalculateDifferenceRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var result = await _shiftManagementService.CalculateDifferenceAsync(
                request.ShiftId,
                request.ActualClosingBalance,
                ct);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating difference for shift {ShiftId}", request.ShiftId);
            return StatusCode(500, new { message = "Lỗi hệ thống khi tính chênh lệch." });
        }
    }

    /// <summary>
    /// UC130 - Add Notes
    /// POST /api/ShiftManagement/closing/notes
    /// </summary>
    [HttpPost("closing/notes")]
    public async Task<IActionResult> AddClosingNotes(
        [FromBody] ShiftClosingNotesRequestDto request,
        CancellationToken ct = default)
    {
        try
        {
            var success = await _shiftManagementService.AddClosingNotesAsync(request, ct);

            if (!success)
            {
                return NotFound(new { message = "Không tìm thấy ca làm việc." });
            }

            return Ok(new { message = "Lưu ghi chú thành công." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding closing notes for shift {ShiftId}", request.ShiftId);
            return StatusCode(500, new { message = "Lỗi hệ thống khi lưu ghi chú." });
        }
    }

    /// <summary>
    /// UC131 - Confirm Closing
    /// POST /api/ShiftManagement/closing/confirm
    /// </summary>
    [HttpPost("closing/confirm")]
    public async Task<IActionResult> ConfirmClosing(
        [FromBody] ShiftClosingConfirmRequestDto request,
        CancellationToken ct = default)
    {
        try
        {
            var result = await _shiftManagementService.ConfirmClosingAsync(request, ct);

            if (!result.Success)
            {
                return BadRequest(new { message = result.Message });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming closing for shift {ShiftId}", request.ShiftId);
            return StatusCode(500, new { message = "Lỗi hệ thống khi xác nhận kết ca." });
        }
    }

    // ========== HANDOVER ==========

    /// <summary>
    /// UC132 - Get available staff for handover
    /// GET /api/ShiftManagement/handover/staff/{currentStaffId}
    /// </summary>
    [HttpGet("handover/staff/{currentStaffId}")]
    public async Task<IActionResult> GetAvailableHandoverStaff(
        int currentStaffId,
        CancellationToken ct = default)
    {
        try
        {
            var staff = await _shiftManagementService.GetAvailableHandoverStaffAsync(currentStaffId, ct);
            return Ok(staff);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available handover staff for {StaffId}", currentStaffId);
            return StatusCode(500, new { message = "Lỗi hệ thống khi lấy danh sách nhân viên." });
        }
    }

    /// <summary>
    /// UC133 - Save handover notes
    /// POST /api/ShiftManagement/handover/notes
    /// </summary>
    [HttpPost("handover/notes")]
    public async Task<IActionResult> SaveHandoverNotes(
        [FromBody] ShiftHandoverNotesRequestDto request,
        CancellationToken ct = default)
    {
        try
        {
            var success = await _shiftManagementService.SaveHandoverNotesAsync(request, ct);

            if (!success)
            {
                return NotFound(new { message = "Không tìm thấy ca làm việc." });
            }

            return Ok(new { message = "Lưu ghi chú giao ca thành công." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving handover notes for shift {ShiftId}", request.ShiftId);
            return StatusCode(500, new { message = "Lỗi hệ thống khi lưu ghi chú giao ca." });
        }
    }

    /// <summary>
    /// UC134 - Verify PIN
    /// POST /api/ShiftManagement/handover/verify-pin
    /// </summary>
    [HttpPost("handover/verify-pin")]
    public async Task<IActionResult> VerifyHandoverPin(
        [FromBody] ShiftHandoverPinRequestDto request,
        CancellationToken ct = default)
    {
        try
        {
            var isValid = await _shiftManagementService.VerifyHandoverPinAsync(request, ct);

            if (!isValid)
            {
                return BadRequest(new { message = "Mã PIN không chính xác." });
            }

            return Ok(new { message = "Xác thực PIN thành công." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying PIN for handover");
            return StatusCode(500, new { message = "Lỗi hệ thống khi xác thực PIN." });
        }
    }

    /// <summary>
    /// UC135 - Create next shift after handover
    /// POST /api/ShiftManagement/handover/create-next
    /// </summary>
    [HttpPost("handover/create-next")]
    public async Task<IActionResult> CreateNextShiftAfterHandover(
        [FromBody] ShiftHandoverCreateNextRequestDto request,
        CancellationToken ct = default)
    {
        try
        {
            var result = await _shiftManagementService.CreateNextShiftAfterHandoverAsync(request, ct);

            if (!result.Success)
            {
                return BadRequest(new { message = result.Message });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating next shift after handover");
            return StatusCode(500, new { message = "Lỗi hệ thống khi tạo ca mới sau giao ca." });
        }
    }

    // ========== DASHBOARD & HISTORY ==========

    /// <summary>
    /// UC121 - View Shift Statistics (Dashboard)
    /// GET /api/ShiftManagement/dashboard/{staffId}
    /// </summary>
    [HttpGet("dashboard/{staffId}")]
    public async Task<IActionResult> GetShiftDashboard(
        int staffId,
        CancellationToken ct = default)
    {
        try
        {
            var dashboard = await _shiftManagementService.GetShiftDashboardAsync(staffId, ct);
            return Ok(dashboard);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting shift dashboard for staff {StaffId}", staffId);
            return StatusCode(500, new { message = "Lỗi hệ thống khi lấy thông tin dashboard." });
        }
    }

    /// <summary>
    /// UC122-124 - Get shift statistics
    /// GET /api/ShiftManagement/{shiftId}/statistics
    /// </summary>
    [HttpGet("{shiftId}/statistics")]
    public async Task<IActionResult> GetShiftStatistics(
        int shiftId,
        CancellationToken ct = default)
    {
        try
        {
            var openingBalance = await _shiftManagementService.GetOpeningBalanceAsync(shiftId, ct);
            var revenue = await _shiftManagementService.GetShiftRevenueAsync(shiftId, ct);
            var orderCount = await _shiftManagementService.GetShiftOrderCountAsync(shiftId, ct);

            return Ok(new
            {
                shiftId,
                openingBalance,
                revenue,
                orderCount
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting statistics for shift {ShiftId}", shiftId);
            return StatusCode(500, new { message = "Lỗi hệ thống khi lấy thống kê ca làm việc." });
        }
    }

    /// <summary>
    /// UC136 - Filter Shift History
    /// GET /api/ShiftManagement/history
    /// </summary>
    [HttpGet("history")]
    public async Task<IActionResult> GetShiftHistory(
        [FromQuery] ShiftFilterDto filter,
        CancellationToken ct = default)
    {
        try
        {
            var history = await _shiftManagementService.GetShiftHistoryAsync(filter, ct);
            return Ok(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting shift history");
            return StatusCode(500, new { message = "Lỗi hệ thống khi lấy lịch sử ca làm việc." });
        }
    }

    /// <summary>
    /// UC137 - View Shift Details
    /// GET /api/ShiftManagement/{shiftId}/details
    /// </summary>
    [HttpGet("{shiftId}/details")]
    public async Task<IActionResult> GetShiftDetails(
        int shiftId,
        CancellationToken ct = default)
    {
        try
        {
            var details = await _shiftManagementService.GetShiftDetailsAsync(shiftId, ct);
            return Ok(details);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = $"Không tìm thấy ca làm việc với ID {shiftId}." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting details for shift {ShiftId}", shiftId);
            return StatusCode(500, new { message = "Lỗi hệ thống khi lấy chi tiết ca làm việc." });
        }
    }

    /// <summary>
    /// UC138 - Export Shift Report
    /// GET /api/ShiftManagement/{shiftId}/export
    /// </summary>
    [HttpGet("{shiftId}/export")]
    public async Task<IActionResult> ExportShiftReport(
        int shiftId,
        CancellationToken ct = default)
    {
        try
        {
            var pdfBytes = await _shiftManagementService.ExportShiftReportAsync(shiftId, ct);

            if (pdfBytes == null || pdfBytes.Length == 0)
            {
                return NotFound(new { message = "Không thể tạo báo cáo." });
            }

            return File(pdfBytes, "application/pdf", $"ShiftReport_{shiftId}_{DateTime.Now:yyyyMMddHHmmss}.pdf");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting report for shift {ShiftId}", shiftId);
            return StatusCode(500, new { message = "Lỗi hệ thống khi xuất báo cáo." });
        }
    }

    // ========== UTILITY ENDPOINTS ==========

    /// <summary>
    /// Get current open shift for staff
    /// GET /api/ShiftManagement/current/{staffId}
    /// </summary>
    [HttpGet("current/{staffId}")]
    public async Task<IActionResult> GetCurrentOpenShift(
        int staffId,
        CancellationToken ct = default)
    {
        try
        {
            var shift = await _shiftManagementService.GetCurrentOpenShiftAsync(staffId, ct);

            if (shift == null)
            {
                return NotFound(new { message = "Không có ca làm việc đang mở." });
            }

            return Ok(shift);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current open shift for staff {StaffId}", staffId);
            return StatusCode(500, new { message = "Lỗi hệ thống." });
        }
    }

    /// <summary>
    /// Check if staff has open shift
    /// GET /api/ShiftManagement/has-open/{staffId}
    /// </summary>
    [HttpGet("has-open/{staffId}")]
    public async Task<IActionResult> HasOpenShift(
        int staffId,
        CancellationToken ct = default)
    {
        try
        {
            var hasOpen = await _shiftManagementService.HasOpenShiftAsync(staffId, ct);
            return Ok(new { hasOpenShift = hasOpen });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking open shift for staff {StaffId}", staffId);
            return StatusCode(500, new { message = "Lỗi hệ thống." });
        }
    }

    /// <summary>
    /// Get shift by ID
    /// GET /api/ShiftManagement/{shiftId}
    /// </summary>
    [HttpGet("{shiftId}")]
    public async Task<IActionResult> GetShiftById(
        int shiftId,
        CancellationToken ct = default)
    {
        try
        {
            var shift = await _shiftManagementService.GetShiftByIdAsync(shiftId, ct);

            if (shift == null)
            {
                return NotFound(new { message = $"Không tìm thấy ca làm việc với ID {shiftId}." });
            }

            return Ok(shift);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting shift {ShiftId}", shiftId);
            return StatusCode(500, new { message = "Lỗi hệ thống." });
        }
    }
}

// Helper request DTOs for endpoints without dedicated DTOs
public record CalculateDifferenceRequest(int ShiftId, decimal ActualClosingBalance);
