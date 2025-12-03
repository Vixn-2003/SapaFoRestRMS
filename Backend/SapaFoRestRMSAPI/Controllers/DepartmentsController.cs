using BusinessAccessLayer.DTOs.Department;
using BusinessAccessLayer.Services.Interfaces;
using DataAccessLayer.Dbcontext;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace SapaFoRestRMSAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DepartmentsController : ControllerBase
    {
        private readonly IDepartmentService _service;

        public DepartmentsController(IDepartmentService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _service.GetAllAsync());
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null) return NotFound();

            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] DepartmentCreateDTO dto)
        {
            var error = await _service.CreateAsync(dto);
            if (error != null) return BadRequest(error);

            return Ok("Tạo phòng ban thành công!");
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] DepartmentUpdateDTO dto)
        {
            var error = await _service.UpdateAsync(id, dto);
            if (error != null) return BadRequest(error);

            return Ok("Cập nhật phòng ban thành công!");
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var error = await _service.DeleteAsync(id);
            if (error != null) return BadRequest(error);

            return Ok("Xóa phòng ban thành công!");
        }
    }
}
