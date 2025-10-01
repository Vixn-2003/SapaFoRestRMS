using DomainAccessLayer.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BusinessLogicLayer.Services.Interfaces
{
    public interface IBrandBannerService
    {
        Task<IEnumerable<BrandBanner>> GetActiveBannersAsync();
        Task<IEnumerable<BrandBanner>> GetAllAsync();
        Task<BrandBanner?> GetByIdAsync(int id);
        Task AddAsync(BrandBanner banner);
        Task UpdateAsync(BrandBanner banner);
        Task DeleteAsync(int id);
    }
}
