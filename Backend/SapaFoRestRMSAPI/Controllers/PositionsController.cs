using BusinessAccessLayer.DTOs.Positions;
using BusinessAccessLayer.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;

namespace SapaFoRestRMSAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,Manager,Owner")]
    public class PositionsController : ControllerBase
    {
        private readonly IPositionService _positionService;

        public PositionsController(IPositionService positionService)
        {
            _positionService = positionService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken ct = default)
        {
            var positions = await _positionService.GetAllAsync(ct);
            return Ok(positions);
        }

        [HttpGet("search")]
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

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id, CancellationToken ct = default)
        {
            var position = await _positionService.GetByIdAsync(id, ct);
            if (position == null)
            {
                return NotFound();
            }
            return Ok(position);
        }

        [HttpPost]
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
        }

        [HttpPut("{id:int}")]
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

        [HttpDelete("{id:int}")]
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

        [HttpPatch("{id:int}/status/{status:int}")]
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


