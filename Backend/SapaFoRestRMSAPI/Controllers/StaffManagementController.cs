using BusinessAccessLayer.DTOs.Staff;
using BusinessAccessLayer.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace SapaFoRestRMSAPI.Controllers
{
    /// <summary>
    /// API Controller for Staff Management Module
    /// UC55 - View List Staff
    /// UC56 - Update Staff
    /// UC57 - Deactivate / Delete Staff
    /// Create Staff
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Manager,Admin")] // Only Manager and Admin can access
    public class StaffManagementController : ControllerBase
    {
        private readonly IStaffManagementService _staffManagementService;

        public StaffManagementController(IStaffManagementService staffManagementService)
        {
            _staffManagementService = staffManagementService;
        }

        /// <summary>
        /// UC55 - View List Staff
        /// GET: api/StaffManagement
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetStaffList(
            [FromQuery] string? searchKeyword,
            [FromQuery] string? position,
            [FromQuery] int? status,
            [FromQuery] int? departmentId,
            [FromQuery] string sortBy = "HireDate",
            [FromQuery] string sortDirection = "desc",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken ct = default)
        {
            try
            {
                var filter = new StaffFilterDto
                {
                    SearchKeyword = searchKeyword,
                    Position = position,
                    Status = status,
                    DepartmentId = departmentId,
                    SortBy = sortBy,
                    SortDirection = sortDirection,
                    Page = page,
                    PageSize = pageSize
                };

                // Get manager's department ID from token (if needed)
                int? managerDepartmentId = null;
                if (User.IsInRole("Manager"))
                {
                    // TODO: Get manager's department from their staff profile
                    // For now, managers can see all departments
                }

                var result = await _staffManagementService.GetStaffListAsync(filter, managerDepartmentId, ct);

                return Ok(new
                {
                    success = true,
                    data = result.Data,
                    page = result.Page,
                    pageSize = result.PageSize,
                    totalCount = result.TotalCount,
                    totalPages = result.TotalPages
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while retrieving staff list.",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Get Staff Detail
        /// GET: api/StaffManagement/{id}
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetStaffDetail(int id, CancellationToken ct = default)
        {
            try
            {
                var staffDetail = await _staffManagementService.GetStaffDetailAsync(id, ct);

                if (staffDetail == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Staff not found or has been deleted."
                    });
                }

                return Ok(new
                {
                    success = true,
                    data = staffDetail
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while retrieving staff details.",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Create New Staff
        /// POST: api/StaffManagement
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateStaff(
            [FromBody] StaffCreateDto dto,
            CancellationToken ct = default)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Invalid request data.",
                        errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                    });
                }

                // Get manager ID from claims
                var managerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(managerIdClaim, out var managerId))
                {
                    return Unauthorized(new
                    {
                        success = false,
                        message = "Manager authentication failed."
                    });
                }

                // Get IP address
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

                // Create staff
                var (success, staffId, message) = await _staffManagementService.CreateStaffAsync(
                    dto,
                    managerId,
                    ipAddress,
                    ct);

                if (!success)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = message
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = message,
                    data = new { staffId = staffId }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while creating staff.",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// UC56 - Update Staff
        /// PUT: api/StaffManagement/{id}
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateStaff(
            int id,
            [FromBody] StaffUpdateDto dto,
            CancellationToken ct = default)
        {
            try
            {
                // Validate ID match
                if (id != dto.StaffId)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Staff ID in URL does not match the ID in request body."
                    });
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Invalid request data.",
                        errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                    });
                }

                // Get manager ID from claims
                var managerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(managerIdClaim, out var managerId))
                {
                    return Unauthorized(new
                    {
                        success = false,
                        message = "Manager authentication failed."
                    });
                }

                // Get IP address
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

                // Update staff
                var (success, message) = await _staffManagementService.UpdateStaffAsync(
                    dto,
                    managerId,
                    ipAddress,
                    ct);

                if (!success)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = message
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while updating staff.",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// UC57 - Deactivate Staff
        /// PUT: api/StaffManagement/{id}/deactivate
        /// </summary>
        [HttpPut("{id}/deactivate")]
        public async Task<IActionResult> DeactivateStaff(
            int id,
            [FromBody] StaffDeactivateDto dto,
            CancellationToken ct = default)
        {
            try
            {
                // Validate ID match
                if (id != dto.StaffId)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Staff ID in URL does not match the ID in request body."
                    });
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Invalid request data.",
                        errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                    });
                }

                // Get manager ID from claims
                var managerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(managerIdClaim, out var managerId))
                {
                    return Unauthorized(new
                    {
                        success = false,
                        message = "Manager authentication failed."
                    });
                }

                // Get IP address
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

                // Deactivate staff
                var (success, message) = await _staffManagementService.DeactivateStaffAsync(
                    dto,
                    managerId,
                    ipAddress,
                    ct);

                if (!success)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = message
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while deactivating staff.",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Get Active Positions for Dropdown
        /// GET: api/StaffManagement/positions
        /// </summary>
        [HttpGet("positions")]
        public async Task<IActionResult> GetActivePositions(CancellationToken ct = default)
        {
            try
            {
                var positions = await _staffManagementService.GetActivePositionsAsync(ct);

                return Ok(new
                {
                    success = true,
                    data = positions
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while retrieving positions.",
                    error = ex.Message
                });
            }
        }
    }
}

