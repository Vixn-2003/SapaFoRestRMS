using BusinessAccessLayer.DTOs.Positions;
using BusinessAccessLayer.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;

namespace SapaFoRestRMSAPI.Controllers
{
    /// <summary>
    /// Controller quản lý Positions
    /// Admin/Owner: Tạo, sửa, xóa Position
    /// Manager: Chỉ xem Position (không thể sửa BaseSalary trực tiếp)
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PositionsController : ControllerBase
    {
        private readonly IPositionService _positionService;

        public PositionsController(IPositionService positionService)
        {
            _positionService = positionService;
        }

        /// <summary>
        /// Admin/Owner/Manager: Xem danh sách Position
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin,Owner,Manager")]
        public async Task<IActionResult> GetAll(CancellationToken ct = default)
        {
            var positions = await _positionService.GetAllAsync(ct);
            return Ok(positions);
        }

        /// <summary>
        /// Admin/Owner/Manager: Tìm kiếm Position
        /// </summary>
        [HttpGet("search")]
        [Authorize(Roles = "Admin,Owner,Manager")]
        public async Task<IActionResult> Search(
            [FromQuery] string? searchTerm,
            [FromQuery] int? status,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            CancellationToken ct = default)
        {
            var request = new PositionSearchRequest
            {
                SearchTerm = searchTerm,
                Status = status,
                Page = page,
                PageSize = pageSize
            };

            var result = await _positionService.SearchAsync(request, ct);
            return Ok(result);
        }

        /// <summary>
        /// Admin/Owner/Manager: Xem chi tiết Position
        /// </summary>
        [HttpGet("{id:int}")]
        [Authorize(Roles = "Admin,Owner,Manager")]
        public async Task<IActionResult> Get(int id, CancellationToken ct = default)
        {
            var position = await _positionService.GetByIdAsync(id, ct);
            if (position == null)
            {
                return NotFound();
            }
            return Ok(position);
        }

        /// <summary>
        /// Tạo Position mới
        /// Lưu ý: Chỉ Owner/Admin mới có quyền tạo Position với BaseSalary
        /// Manager muốn thay đổi BaseSalary phải tạo SalaryChangeRequest
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin,Owner")]
        public async Task<IActionResult> Create([FromBody] PositionCreateRequest request, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var position = await _positionService.CreateAsync(request, ct);
                return CreatedAtAction(nameof(Get), new { id = position.PositionId }, position);
            }
            catch (System.InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (System.ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Admin/Owner: Cập nhật Position
        /// Lưu ý: BaseSalary KHÔNG được cập nhật ở đây
        /// Manager muốn thay đổi BaseSalary phải tạo SalaryChangeRequest
        /// </summary>
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin,Owner")]
        public async Task<IActionResult> Update(int id, [FromBody] PositionUpdateRequest request, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                await _positionService.UpdateAsync(id, request, ct);
                return NoContent();
            }
            catch (System.InvalidOperationException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Admin/Owner: Xóa Position
        /// </summary>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin,Owner")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct = default)
        {
            try
            {
                await _positionService.DeleteAsync(id, ct);
                return NoContent();
            }
            catch (System.InvalidOperationException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Admin/Owner: Thay đổi trạng thái Position
        /// </summary>
        [HttpPatch("{id:int}/status/{status:int}")]
        [Authorize(Roles = "Admin,Owner")]
        public async Task<IActionResult> ChangeStatus(int id, int status, CancellationToken ct = default)
        {
            try
            {
                await _positionService.ChangeStatusAsync(id, status, ct);
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


