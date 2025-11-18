using BusinessAccessLayer.DTOs.RestaurantIntroDto;
using BusinessAccessLayer.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace SapaFoRestRMSAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RestaurantIntroController : ControllerBase
    {
        private readonly IRestaurantIntroService _service;

        public RestaurantIntroController(IRestaurantIntroService service)
        {
            _service = service;
        }

        // Admin: List tất cả intro
        [HttpGet("admin/list")]
        public async Task<IActionResult> GetAll()
        {
            var data = await _service.GetAllAsync();
            return Ok(data);
        }

        // Customer: Xem intro đang hoạt động
        [HttpGet("active")]
        public async Task<IActionResult> GetActive()
        {
            var data = await _service.GetActiveAsync();
            return Ok(data);
        }

        // Admin: Xem chi tiết intro
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var data = await _service.GetByIdAsync(id);
            return data == null ? NotFound() : Ok(data);
        }

        // Admin: Thêm mới
        [HttpPost("add")]
        public async Task<IActionResult> Create([FromForm] CreateRestaurantIntroDto dto)
        {
            var result = await _service.CreateAsync(dto);
            return Ok(result);
        }

        // Admin: Cập nhật
        [HttpPut("update/{id}")]
        public async Task<IActionResult> Update(int id, [FromForm] UpdateRestaurantIntroDto dto)
        {
            var success = await _service.UpdateAsync(id, dto);
            return success ? Ok("Updated successfully") : NotFound("Not found");
        }

        // Admin: Xóa
        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _service.DeleteAsync(id);
            return success ? Ok("Deleted successfully") : NotFound("Not found");
        }
    }
}
