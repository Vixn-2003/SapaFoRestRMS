using DomainAccessLayer.Models;
using System.Collections.Generic;

namespace DataAccessLayer.Repositories.Interfaces
{
    public interface IBrandBannerRepository : IRepository<BrandBanner>
    {
        IEnumerable<BrandBanner> GetActiveBanners();
        IEnumerable<BrandBanner> GetAllWithUser();
    }
}
