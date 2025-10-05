using BusinessLogicLayer.Services.Interfaces;
using DataAccessLayer.Repositories.Interfaces;
using DomainAccessLayer.Models;
using System.Collections.Generic;
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

        public async Task<IEnumerable<BrandBanner>> GetActiveBannersAsync()
        {
            return await Task.FromResult(_bannerRepository.GetActiveBanners());
        }

        public async Task<IEnumerable<BrandBanner>> GetAllAsync()
        {
            return await _bannerRepository.GetAllAsync();
        }

        public async Task<BrandBanner?> GetByIdAsync(int id)
        {
            return await _bannerRepository.GetByIdAsync(id);
        }

        public async Task AddAsync(BrandBanner banner)
        {
            banner.CreatedBy = 3;
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
