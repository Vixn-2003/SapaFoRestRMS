using System.Threading;
using System.Threading.Tasks;
using BusinessAccessLayer.DTOs.Users;
using BusinessAccessLayer.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SapaFoRestRMSAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,Manager")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken ct)
        {
            var users = await _userService.GetAllAsync(ct);
            return Ok(users);
        }

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

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id, CancellationToken ct)
        {
            var user = await _userService.GetByIdAsync(id, ct);
            if (user == null) return NotFound();
            return Ok(user);
        }

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
    }
}


