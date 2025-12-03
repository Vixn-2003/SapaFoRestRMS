using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using BusinessAccessLayer.DTOs.ShiftManagement;
using BusinessAccessLayer.Services.Interfaces;

namespace SapaFoRestRMSAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ShiftManagementController : ControllerBase
{
    private readonly IShiftManagementService _shiftService;
    private readonly ILogger<ShiftManagementController> _logger;

    public ShiftManagementController(
        IShiftManagementService shiftService,
        ILogger<ShiftManagementController> logger)
    {
        _shiftService = shiftService;
        _logger = logger;
    }

    /// <summary>
    /// Lấy ca làm việc hiện tại
    /// GET: api/ShiftManagement/current
    /// </summary>
    [HttpGet("current")]
    public async Task<ActionResult<ShiftDashboardDto>> GetCurrentShift(CancellationToken ct = default)
    {
        try
        {
            var staffId = GetStaffIdFromToken();
            if (staffId == 0)
            {
                return BadRequest(new { message = "Không xác định được Staff ID" });
            }

            var shift = await _shiftService.GetCurrentShiftAsync(staffId, ct);
            
            if (shift == null)
            {
                return NotFound(new { message = "Không có ca làm việc đang mở" });
            }

            return Ok(shift);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current shift");
            return StatusCode(500, new { message = "Lỗi server khi lấy thông tin ca làm việc" });
        }
    }

    /// <summary>
    /// Mở ca làm việc mới
    /// POST: api/ShiftManagement/open
    /// </summary>
    [HttpPost("open")]
    public async Task<ActionResult<ShiftResponseDto>> OpenShift([FromBody] OpenShiftRequestDto request, CancellationToken ct = default)
    {
        try
        {
            if (request == null)
            {
                return BadRequest(new { message = "Request không hợp lệ" });
            }

            // Nếu không truyền StaffId, lấy từ token
            if (request.StaffId == 0)
            {
                request.StaffId = GetStaffIdFromToken();
            }

            if (request.StaffId == 0)
            {
                return BadRequest(new { message = "Không xác định được Staff ID" });
            }

            var result = await _shiftService.OpenShiftAsync(request, ct);
            
            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error opening shift");
            return StatusCode(500, new ShiftResponseDto 
            { 
                Success = false, 
                Message = "Lỗi server khi mở ca làm việc" 
            });
        }
    }

    /// <summary>
    /// Kết ca làm việc
    /// POST: api/ShiftManagement/close
    /// </summary>
    [HttpPost("close")]
    public async Task<ActionResult<ShiftResponseDto>> CloseShift([FromBody] CloseShiftRequestDto request, CancellationToken ct = default)
    {
        try
        {
            if (request == null)
            {
                return BadRequest(new { message = "Request không hợp lệ" });
            }

            var result = await _shiftService.CloseShiftAsync(request, ct);
            
            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error closing shift");
            return StatusCode(500, new ShiftResponseDto 
            { 
                Success = false, 
                Message = "Lỗi server khi kết ca làm việc" 
            });
        }
    }

    /// <summary>
    /// Giao ca cho nhân viên khác
    /// POST: api/ShiftManagement/handover
    /// </summary>
    [HttpPost("handover")]
    public async Task<ActionResult<ShiftResponseDto>> HandoverShift([FromBody] HandoverShiftRequestDto request, CancellationToken ct = default)
    {
        try
        {
            if (request == null)
            {
                return BadRequest(new { message = "Request không hợp lệ" });
            }

            var result = await _shiftService.HandoverShiftAsync(request, ct);
            
            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handing over shift");
            return StatusCode(500, new ShiftResponseDto 
            { 
                Success = false, 
                Message = "Lỗi server khi giao ca" 
            });
        }
    }

    /// <summary>
    /// Lấy lịch sử ca làm việc
    /// GET: api/ShiftManagement/history
    /// </summary>
    [HttpGet("history")]
    public async Task<ActionResult> GetShiftHistory(
        [FromQuery] string? fromDate = null,
        [FromQuery] string? toDate = null,
        CancellationToken ct = default)
    {
        try
        {
            var staffId = GetStaffIdFromToken();
            if (staffId == 0)
            {
                return BadRequest(new { message = "Không xác định được Staff ID" });
            }

            var from = string.IsNullOrEmpty(fromDate) 
                ? DateOnly.FromDateTime(DateTime.Now.AddMonths(-1))
                : DateOnly.Parse(fromDate);
            
            var to = string.IsNullOrEmpty(toDate)
                ? DateOnly.FromDateTime(DateTime.Now)
                : DateOnly.Parse(toDate);

            var history = await _shiftService.GetShiftHistoryAsync(staffId, from, to, ct);
            
            return Ok(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting shift history");
            return StatusCode(500, new { message = "Lỗi server khi lấy lịch sử ca làm việc" });
        }
    }

    /// <summary>
    /// Lấy chi tiết ca làm việc
    /// GET: api/ShiftManagement/{shiftId}
    /// </summary>
    [HttpGet("{shiftId}")]
    public async Task<ActionResult> GetShiftDetails(int shiftId, CancellationToken ct = default)
    {
        try
        {
            var shift = await _shiftService.GetShiftDetailsAsync(shiftId, ct);
            
            if (shift == null)
            {
                return NotFound(new { message = "Không tìm thấy ca làm việc" });
            }

            return Ok(shift);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting shift details");
            return StatusCode(500, new { message = "Lỗi server khi lấy chi tiết ca làm việc" });
        }
    }

    /// <summary>
    /// Kiểm tra xem có ca làm việc đang mở không
    /// GET: api/ShiftManagement/has-open-shift
    /// </summary>
    [HttpGet("has-open-shift")]
    public async Task<ActionResult<bool>> HasOpenShift(CancellationToken ct = default)
    {
        try
        {
            var staffId = GetStaffIdFromToken();
            if (staffId == 0)
            {
                return BadRequest(new { message = "Không xác định được Staff ID" });
            }

            var hasOpen = await _shiftService.HasOpenShiftAsync(staffId, ct);
            
            return Ok(new { hasOpenShift = hasOpen });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking open shift");
            return StatusCode(500, new { message = "Lỗi server khi kiểm tra ca làm việc" });
        }
    }

    // Helper method to extract StaffId from JWT token
    private int GetStaffIdFromToken()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return 0;
        }

        // TODO: Get StaffId from UserId via database lookup
        // For now, assume userId = staffId (need to implement proper mapping)
        return userId;
    }
}

