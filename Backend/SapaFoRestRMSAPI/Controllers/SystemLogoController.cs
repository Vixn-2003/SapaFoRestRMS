using BusinessAccessLayer.DTOs;
using BusinessAccessLayer.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using SapaFoRestRMSAPI.Services;
using System.Threading.Tasks;

namespace SapaFoRestRMSAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SystemLogoController : ControllerBase
    {
        private readonly ISystemLogoService _logoService;
        private readonly CloudinaryService _cloudinaryService;

        public SystemLogoController(ISystemLogoService logoService, CloudinaryService cloudinaryService)
        {
            _logoService = logoService;
            _cloudinaryService = cloudinaryService;
        }
        [HttpGet("all")]
        public IActionResult GetAllLogos()
        {
            var logos = _logoService.GetAllLogos();
            return Ok(logos);
        }


        [HttpGet("active")]
        public IActionResult GetActiveLogos()
        {
            var logos = _logoService.GetActiveLogos();
            return Ok(logos);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetLogoById(int id)
        {
            var logo = await _logoService.GetByIdAsync(id);
            if (logo == null) return NotFound();
            return Ok(logo);
        }

        [HttpPost]
        public async Task<IActionResult> AddLogo([FromForm] SystemLogoDto dto, int userId = 3)
        {
            if (dto.File != null)
            {
                dto.LogoUrl = await _cloudinaryService.UploadFileAsync(dto.File);
            }

            var newLogo = await _logoService.AddLogoAsync(dto, userId);
            return CreatedAtAction(nameof(GetLogoById), new { id = newLogo.LogoId }, newLogo);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateLogo(int id, [FromForm] SystemLogoDto dto, [FromQuery] int userId = 3)
        {
            if (id != dto.LogoId && dto.LogoId == 0)
                dto.LogoId = id;

            //  Upload ảnh mới nếu có
            if (dto.File != null && dto.File.Length > 0)
            {
                var uploadUrl = await _cloudinaryService.UploadFileAsync(dto.File);
                dto.LogoUrl = uploadUrl; 
            }

            //  Nếu không upload ảnh mới, giữ nguyên URL cũ để tránh null
            else
            {
                var existingLogo = await _logoService.GetByIdAsync(id);
                if (existingLogo == null)
                    return NotFound(new { message = "Không tìm thấy logo cần cập nhật." });

                dto.LogoUrl = existingLogo.LogoUrl; 
            }

            var success = await _logoService.UpdateLogoAsync(dto, userId);

            if (!success)
                return NotFound(new { message = "Cập nhật không thành công." });

            return Ok(new { message = "Cập nhật logo thành công!" });
        }



        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteLogo(int id)
        {
            var success = await _logoService.DeleteLogoAsync(id);
            if (!success) return NotFound();
            return NoContent();
        }
    }
}
