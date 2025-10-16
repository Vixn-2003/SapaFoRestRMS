using BusinessAccessLayer.DTOs;
using BusinessLogicLayer.Services.Interfaces;
using DomainAccessLayer.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SapaFoRestRMSAPI.Services;

namespace SapaFoRestRMSAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BrandBannerController : ControllerBase
    {
        private readonly IBrandBannerService _bannerService;
        private readonly CloudinaryService _cloudinaryService;

        public BrandBannerController(IBrandBannerService bannerService, CloudinaryService cloudinaryService)
        {
            _bannerService = bannerService;
            _cloudinaryService = cloudinaryService;
        }

        [HttpGet("active")]
        public async Task<ActionResult<IEnumerable<BrandBannerDto>>> GetActiveBanners()
        {
            var banners = await _bannerService.GetActiveBannersAsync();
            return Ok(banners);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<BrandBannerDto>>> GetAll()
        {
            var banners = await _bannerService.GetAllAsync();
            await AutoUpdateBannerStatus(banners);
            return Ok(banners);
        }
        [HttpGet("filter")]
        public async Task<IActionResult> Filter([FromQuery] string? status, [FromQuery] string? title, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 7)
        {
            var banners = await _bannerService.GetAllAsync();
            await AutoUpdateBannerStatus(banners);
            if (!string.IsNullOrEmpty(status))
                banners = banners.Where(b => b.Status != null && b.Status.Equals(status, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(title))
                banners = banners.Where(b => b.Title != null && b.Title.Contains(title, StringComparison.OrdinalIgnoreCase));

            var totalItems = banners.Count();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var pagedData = banners
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var result = new
            {
                Data = pagedData,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages
            };

            return Ok(result);
        }
        private async Task AutoUpdateBannerStatus(IEnumerable<BrandBannerDto> banners)
        {
            var now = DateTime.Now;
            foreach (var dto in banners)
            {
                if (dto.EndDate.HasValue && dto.EndDate.Value.ToDateTime(TimeOnly.MinValue) < now && dto.Status == "Active")
                {
                    var entity = await _bannerService.GetByIdAsync(dto.BannerId);
                    if (entity != null && entity.Status == "Active")
                    {
                        entity.Status = "Inactive";
                        await _bannerService.UpdateAsync(entity);
                    }
                }
            }
        }


        [HttpGet("{id}")]
        public async Task<ActionResult<BrandBanner>> GetById(int id)
        {
            var banner = await _bannerService.GetByIdAsync(id);
            if (banner == null) return NotFound();
            return Ok(banner);
        }


        [HttpPost]
        public async Task<ActionResult> Add([FromForm] BrandBannerUpdateDto dto)
        {
            string? imageUrl = null;
            if (dto.ImageFile != null)
            {
                imageUrl = await _cloudinaryService.UploadFileAsync(dto.ImageFile);
            }

            var banner = new BrandBanner
            {
                Title = dto.Title,
                ImageUrl = imageUrl,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                Status = dto.Status,
                CreatedBy = 3
            };

            await _bannerService.AddAsync(banner);
            return CreatedAtAction(nameof(GetById), new { id = banner.BannerId }, banner);
        }
        [HttpGet("statuses")]
        public IActionResult GetBannerStatuses()
        {
            var statuses = new List<string> { "Active", "Inactive" };
            return Ok(statuses);
        }


        [HttpPut("{id}")]
        public async Task<ActionResult> Update(int id, [FromForm] BrandBannerUpdateDto dto)
        {
            if (id != dto.BannerId) return BadRequest();

            var banner = await _bannerService.GetByIdAsync(id);
            if (banner == null) return NotFound();

            banner.Title = dto.Title;
            banner.StartDate = dto.StartDate;
            banner.EndDate = dto.EndDate;
            banner.Status = dto.Status;
            banner.CreatedBy = 3;

            if (dto.ImageFile != null)
            {
                banner.ImageUrl = await _cloudinaryService.UploadFileAsync(dto.ImageFile);
            }

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
