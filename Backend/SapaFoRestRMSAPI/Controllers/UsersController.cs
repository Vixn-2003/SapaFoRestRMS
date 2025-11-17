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
    [Authorize(Roles = "Admin")]
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
        public async Task<IActionResult> GetAll(CancellationToken ct)
        {
            var users = await _userService.GetAllAsync(ct);
            return Ok(users);
        }

        /// <summary>
        /// Admin: Tìm kiếm users
        /// </summary>
        [HttpGet("search")]
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
        public async Task<IActionResult> Get(int id, CancellationToken ct)
        {
            var user = await _userService.GetByIdAsync(id, ct);
            if (user == null) return NotFound();
            return Ok(user);
        }

        /// <summary>
        /// Admin: Tạo user mới
        /// </summary>
        [HttpPost]
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
        /// </summary>
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UserUpdateRequest request, CancellationToken ct)
        {
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
        public async Task<IActionResult> UpdateProfile([FromBody] UserProfileUpdateRequest request, CancellationToken ct)
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


