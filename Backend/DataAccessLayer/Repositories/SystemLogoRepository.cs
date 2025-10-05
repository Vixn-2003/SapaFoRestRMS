using DataAccessLayer.Dbcontext;
using DataAccessLayer.Repositories.Interfaces;
using DomainAccessLayer.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories
{
    public class SystemLogoRepository : Repository<SystemLogo>, ISystemLogoRepository
    {
        private readonly SapaFoRestRmsContext _context;

        public SystemLogoRepository(SapaFoRestRmsContext context) : base(context)
        {
            _context = context;
        }
        public IEnumerable<SystemLogo> GetAll()
        {
            return _context.SystemLogos.Include(l => l.CreatedByNavigation).ToList();
        }

        public IEnumerable<SystemLogo> GetActiveLogos()
        {
            return _context.SystemLogos.Include(l => l.CreatedByNavigation).Where(l => l.IsActive == true).ToList();
        }
        public async Task<SystemLogo?> GetByIdAsync(int id)
        {
            return await _context.SystemLogos.Include(l => l.CreatedByNavigation).FirstOrDefaultAsync(l => l.LogoId == id);
        }

        public async Task AddAsync(SystemLogo logo)
        {
            await _context.SystemLogos.AddAsync(logo);
        }

        public void Update(SystemLogo logo)
        {
            _context.SystemLogos.Update(logo);
        }

        public void Delete(SystemLogo logo)
        {
            _context.SystemLogos.Remove(logo);
        }
    }
}
