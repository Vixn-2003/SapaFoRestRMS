using System.Threading;
using System.Threading.Tasks;
using BusinessAccessLayer.DTOs.Users;
using BusinessAccessLayer.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SapaFoRestRMSAPI.Controllers
{
    /// <summary>
    /// Controller quản lý Users
    /// Chỉ Admin có quyền quản lý users (tạo, sửa, xóa)
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // default: authenticated
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        /// <summary>
        /// Admin: Lấy danh sách tất cả users
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAll(CancellationToken ct)
        {
            var users = await _userService.GetAllAsync(ct);
            return Ok(users);
        }

        /// <summary>
        /// Admin: Tìm kiếm users
        /// </summary>
        [HttpGet("search")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Search(
            [FromQuery] string? searchTerm = null,
            [FromQuery] int? roleId = null,
            [FromQuery] int? status = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string sortBy = "FullName",
            [FromQuery] string sortOrder = "asc",
            CancellationToken ct = default)
        {
            var request = new UserSearchRequest
            {
                SearchTerm = searchTerm,
                RoleId = roleId,
                Status = status,
                Page = page,
                PageSize = pageSize,
                SortBy = sortBy,
                SortOrder = sortOrder
            };

            var result = await _userService.SearchAsync(request, ct);
            return Ok(result);
        }

        /// <summary>
        /// Admin: Lấy chi tiết user
        /// </summary>
        [HttpGet("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Get(int id, CancellationToken ct)
        {
            var user = await _userService.GetByIdAsync(id, ct);
            if (user == null) return NotFound();
            return Ok(user);
        }

        /// <summary>
        /// Admin: Lấy chi tiết user (bao gồm metadata)
        /// </summary>
        [HttpGet("{id:int}/details")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetDetails(int id, CancellationToken ct)
        {
            var user = await _userService.GetDetailsAsync(id, ct);
            if (user == null) return NotFound();
            return Ok(user);
        }

        /// <summary>
        /// Admin: Tạo user mới
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] UserCreateRequest request, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var user = await _userService.CreateAsync(request, ct);
                return CreatedAtAction(nameof(Get), new { id = user.UserId }, user);
            }
            catch (System.InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Admin: Cập nhật user
        /// Supports both JSON (FromBody) and multipart/form-data (FromForm) for file upload
        /// </summary>
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromForm] UserUpdateRequest? formRequest, [FromBody] UserUpdateRequest? jsonRequest, CancellationToken ct)
        {
            // Prefer form request if available (for file upload), otherwise use JSON request
            var request = formRequest ?? jsonRequest;
            
            if (request == null)
            {
                return BadRequest(new { message = "Request body is required" });
            }

            // Note: RoleId in request is ignored in UpdateAsync (preserved from original user)
            // The id parameter is the user ID to update

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                await _userService.UpdateAsync(id, request, ct);
                return NoContent();
            }
            catch (System.InvalidOperationException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Admin: Xóa user
        /// </summary>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            try
            {
                await _userService.DeleteAsync(id, ct);
                return NoContent();
            }
            catch (System.InvalidOperationException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Admin: Thay đổi trạng thái user
        /// </summary>
        [HttpPatch("{id:int}/status/{status:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ChangeStatus(int id, int status, CancellationToken ct)
        {
            try
            {
                await _userService.ChangeStatusAsync(id, status, ct);
                return NoContent();
            }
            catch (System.ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (System.InvalidOperationException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Admin: Đặt lại mật khẩu user
        /// </summary>
        [HttpPost("{id:int}/reset-password")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ResetPassword(int id, [FromBody] ResetUserPasswordRequest request, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var newPassword = await _userService.ResetPasswordAsync(id, request, ct);
                return Ok(new { newPassword });
            }
            catch (System.InvalidOperationException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (System.ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("profile")]
        [Authorize]
        public async Task<IActionResult> GetProfile(CancellationToken ct)
        {
            // Try both claim types for compatibility
            var userIdClaim = User.FindFirst("userId")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            var user = await _userService.GetByIdAsync(userId, ct);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            return Ok(user);
        }

        [HttpPut("profile")]
        [Authorize]
        public async Task<IActionResult> UpdateProfile([FromForm] UserProfileUpdateRequest request, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Try both claim types for compatibility
            var userIdClaim = User.FindFirst("userId")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            try
            {
                var updatedUser = await _userService.UpdateProfileAsync(userId, request, ct);
                return Ok(updatedUser);
            }
            catch (System.InvalidOperationException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }
    }
}


