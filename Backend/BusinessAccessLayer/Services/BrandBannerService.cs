using BusinessAccessLayer.DTOs;
using BusinessLogicLayer.Services.Interfaces;
using DataAccessLayer.Repositories.Interfaces;
using DomainAccessLayer.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BusinessLogicLayer.Services
{
    public class BrandBannerService : IBrandBannerService
    {
        private readonly IBrandBannerRepository _bannerRepository;

        public BrandBannerService(IBrandBannerRepository bannerRepository)
        {
            _bannerRepository = bannerRepository;
        }

        public async Task<IEnumerable<BrandBannerDto>> GetAllAsync()
        {
            var banners = _bannerRepository.GetAllWithUser();

            var result = banners.Select(b => new BrandBannerDto
            {
                BannerId = b.BannerId,
                Title = b.Title,
                ImageUrl = b.ImageUrl,
                StartDate = b.StartDate,
                EndDate = b.EndDate,
                Status = b.Status,
                CreatedBy = b.CreatedBy,
                CreatedByName = b.CreatedByNavigation?.FullName
            });

            return await Task.FromResult(result);
        }

        public async Task<IEnumerable<BrandBannerDto>> GetActiveBannersAsync()
        {
            var banners = _bannerRepository.GetActiveBanners();

            var result = banners.Select(b => new BrandBannerDto
            {
                BannerId = b.BannerId,
                Title = b.Title,
                ImageUrl = b.ImageUrl,
                StartDate = b.StartDate,
                EndDate = b.EndDate,
                Status = b.Status,
                CreatedBy = b.CreatedBy,
                CreatedByName = b.CreatedByNavigation?.FullName
            });

            return await Task.FromResult(result);
        }

        public async Task<BrandBanner?> GetByIdAsync(int id)
        {
            return await _bannerRepository.GetByIdAsync(id);
        }

        public async Task AddAsync(BrandBanner banner)
        {
            banner.CreatedBy = 3; // TODO: sau này lấy từ user đăng nhập
            await _bannerRepository.AddAsync(banner);
            await _bannerRepository.SaveChangesAsync();
        }

        public async Task UpdateAsync(BrandBanner banner)
        {
            await _bannerRepository.UpdateAsync(banner);
            await _bannerRepository.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            await _bannerRepository.DeleteAsync(id);
            await _bannerRepository.SaveChangesAsync();
        }
    }
}
