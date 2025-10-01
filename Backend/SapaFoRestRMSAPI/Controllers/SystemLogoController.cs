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
        public async Task<IActionResult> UpdateLogo(int id, [FromForm] SystemLogoDto dto, int userId = 3)
        {
            dto.LogoId = id;

            if (dto.File != null)
            {
                dto.LogoUrl = await _cloudinaryService.UploadFileAsync(dto.File);
            }

            var success = await _logoService.UpdateLogoAsync(dto, userId);
            if (!success) return NotFound();
            return NoContent();
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
