using BusinessAccessLayer.DTOs.DayTypeDTOs;
using BusinessAccessLayer.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace SapaFoRestRMSAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DayTypeController : ControllerBase
    {
        private readonly IDayTypeService _service;

        public DayTypeController(IDayTypeService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _service.GetAllAsync());
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null) return NotFound("Không tìm thấy DayType.");
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create(DayTypeCreateDTO dto)
        {
            var (success, message) = await _service.CreateAsync(dto);
            if (!success) return BadRequest(message);
            return Ok(message);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, DayTypeUpdateDTO dto)
        {
            var (success, message) = await _service.UpdateAsync(id, dto);
            if (!success) return BadRequest(message);
            return Ok(message);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var (success, message) = await _service.DeleteAsync(id);
            if (!success) return BadRequest(message);
            return Ok(message);
        }
    }
}
