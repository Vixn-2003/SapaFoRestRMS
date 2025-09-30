using BusinessAccessLayer.DTOs;

using BusinessLogicLayer.Services.Interfaces;
using DomainAccessLayer.Models;
using Microsoft.AspNetCore.Mvc;

namespace SapaFoRestRMSAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BrandBannerController : ControllerBase
    {
        private readonly IBrandBannerService _bannerService;

        public BrandBannerController(IBrandBannerService bannerService)
        {
            _bannerService = bannerService;
        }

        [HttpGet("active")]
        public async Task<ActionResult<IEnumerable<BrandBanner>>> GetActiveBanners()
        {
            var banners = await _bannerService.GetActiveBannersAsync();
            return Ok(banners);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<BrandBanner>>> GetAll()
        {
            var banners = await _bannerService.GetAllAsync();
            return Ok(banners);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<BrandBanner>> GetById(int id)
        {
            var banner = await _bannerService.GetByIdAsync(id);
            if (banner == null) return NotFound();
            return Ok(banner);
        }

        [HttpPost]
        public async Task<ActionResult> Add([FromBody] BrandBannerUpdateDto dto)
        {
            var banner = new BrandBanner
            {
                Title = dto.Title,
                ImageUrl = dto.ImageUrl,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                Status = dto.Status,
                CreatedBy = 1, // tạm thời fix user id = 1
               
            };

            await _bannerService.AddAsync(banner);
            return CreatedAtAction(nameof(GetById), new { id = banner.BannerId }, banner);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> Update(int id, [FromBody] BrandBannerUpdateDto dto)
        {
            if (id != dto.BannerId) return BadRequest();

            var banner = await _bannerService.GetByIdAsync(id);
            if (banner == null) return NotFound();

            banner.Title = dto.Title;
            banner.ImageUrl = dto.ImageUrl;
            banner.StartDate = dto.StartDate;
            banner.EndDate = dto.EndDate;
            banner.Status = dto.Status;
            banner.CreatedBy = 1; // tạm thời fix user id = 1
          

            await _bannerService.UpdateAsync(banner);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            await _bannerService.DeleteAsync(id);
            return NoContent();
        }
    }
}
