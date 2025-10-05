using DataAccessLayer.Dbcontext;
using DataAccessLayer.Repositories.Interfaces;
using DomainAccessLayer.Models;
using System.Collections.Generic;
using System.Linq;

namespace DataAccessLayer.Repositories
{
    public class BrandBannerRepository : Repository<BrandBanner>, IBrandBannerRepository
    {
        private readonly SapaFoRestRmsContext _context;

        public BrandBannerRepository(SapaFoRestRmsContext context) : base(context)
        {
            _context = context;
        }

        public IEnumerable<BrandBanner> GetActiveBanners()
        {
            return _context.BrandBanners
                .Where(b => b.Status == "Active")
                .ToList();
        }
    }
}
