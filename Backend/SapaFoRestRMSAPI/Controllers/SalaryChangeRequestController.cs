using BusinessAccessLayer.DTOs.Positions;
using BusinessAccessLayer.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace SapaFoRestRMSAPI.Controllers;

/// <summary>
/// Controller xử lý các API liên quan đến yêu cầu thay đổi lương
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SalaryChangeRequestController : ControllerBase
{
    private readonly ISalaryChangeRequestService _salaryChangeRequestService;

    public SalaryChangeRequestController(ISalaryChangeRequestService salaryChangeRequestService)
    {
        _salaryChangeRequestService = salaryChangeRequestService;
    }

    /// <summary>
    /// Manager: Tạo yêu cầu thay đổi BaseSalary cho Position
    /// Flow: Manager tạo yêu cầu → Owner xem và phê duyệt/từ chối → BaseSalary được cập nhật khi approve
    /// POST /api/salarychangerequest
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> CreateRequest([FromBody] CreateSalaryChangeRequestDto request, CancellationToken ct = default)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Lấy UserId từ claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { message = "Không thể xác định người dùng" });
            }

            var result = await _salaryChangeRequestService.CreateRequestAsync(request, userId, ct);
            return CreatedAtAction(nameof(GetById), new { id = result.RequestId }, result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Lỗi khi tạo yêu cầu", error = ex.Message });
        }
    }

    /// <summary>
    /// Owner: Lấy danh sách yêu cầu chờ phê duyệt
    /// GET /api/salarychangerequest/pending
    /// </summary>
    [HttpGet("pending")]
    [Authorize(Roles = "Owner")]
    public async Task<IActionResult> GetPendingRequests(CancellationToken ct = default)
    {
        try
        {
            var requests = await _salaryChangeRequestService.GetPendingRequestsAsync(ct);
            return Ok(requests);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Lỗi khi lấy danh sách yêu cầu", error = ex.Message });
        }
    }

    /// <summary>
    /// Owner: Lấy tất cả yêu cầu (có thể lọc theo status)
    /// GET /api/salarychangerequest?status=Approved
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Owner")]
    public async Task<IActionResult> GetAllRequests([FromQuery] string? status = null, CancellationToken ct = default)
    {
        try
        {
            var requests = await _salaryChangeRequestService.GetAllRequestsAsync(status, ct);
            return Ok(requests);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Lỗi khi lấy danh sách yêu cầu", error = ex.Message });
        }
    }

    /// <summary>
    /// Owner: Phê duyệt hoặc từ chối yêu cầu
    /// PUT /api/salarychangerequest/review
    /// </summary>
    [HttpPut("review")]
    [Authorize(Roles = "Owner")]
    public async Task<IActionResult> ReviewRequest([FromBody] ReviewSalaryChangeRequestDto request, CancellationToken ct = default)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Lấy UserId từ claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { message = "Không thể xác định người dùng" });
            }

            var result = await _salaryChangeRequestService.ReviewRequestAsync(request, userId, ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Lỗi khi xử lý yêu cầu", error = ex.Message });
        }
    }

    /// <summary>
    /// Manager: Lấy danh sách yêu cầu của mình
    /// GET /api/salarychangerequest/my-requests
    /// </summary>
    [HttpGet("my-requests")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> GetMyRequests(CancellationToken ct = default)
    {
        try
        {
            // Lấy UserId từ claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { message = "Không thể xác định người dùng" });
            }

            var requests = await _salaryChangeRequestService.GetMyRequestsAsync(userId, ct);
            return Ok(requests);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Lỗi khi lấy danh sách yêu cầu", error = ex.Message });
        }
    }

    /// <summary>
    /// Owner: Lấy thống kê yêu cầu thay đổi lương
    /// GET /api/salarychangerequest/statistics
    /// </summary>
    [HttpGet("statistics")]
    [Authorize(Roles = "Owner")]
    public async Task<IActionResult> GetStatistics(CancellationToken ct = default)
    {
        try
        {
            var statistics = await _salaryChangeRequestService.GetStatisticsAsync(ct);
            return Ok(statistics);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Lỗi khi lấy thống kê", error = ex.Message });
        }
    }

    /// <summary>
    /// Lấy chi tiết yêu cầu
    /// GET /api/salarychangerequest/{id}
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Roles = "Manager,Owner")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct = default)
    {
        try
        {
            var request = await _salaryChangeRequestService.GetByIdAsync(id, ct);
            
            if (request == null)
            {
                return NotFound(new { message = $"Không tìm thấy yêu cầu với ID: {id}" });
            }

            return Ok(request);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Lỗi khi lấy chi tiết yêu cầu", error = ex.Message });
        }
    }
}

