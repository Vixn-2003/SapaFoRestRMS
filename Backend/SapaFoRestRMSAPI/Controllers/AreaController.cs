using BusinessAccessLayer.DTOs;
using BusinessAccessLayer.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace SapaFoRestRMSAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AreaController : ControllerBase
    {
        private readonly IAreaService _service;

        public AreaController(IAreaService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? searchName, [FromQuery] int? floor, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var (areas, total) = await _service.GetAllAsync(searchName, floor, page, pageSize);
            return Ok(new { data = areas, total });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var area = await _service.GetByIdAsync(id);
            if (area == null) return NotFound();
            return Ok(area);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] AreaDto dto)
        {
            var result = await _service.CreateAsync(dto);
            if (!result.Success) return BadRequest(result.Message);
            return Ok(result.Message);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] AreaDto dto)
        {
            if (id != dto.AreaId)
                return BadRequest("ID không khớp.");

            var result = await _service.UpdateAsync(dto);
            if (!result.Success) return BadRequest(result.Message);
            return Ok(result.Message);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _service.DeleteAsync(id);
            if (!result.Success) return BadRequest(result.Message);
            return Ok(result.Message);
        }
    }
}
