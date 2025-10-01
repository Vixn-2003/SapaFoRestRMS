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
    public class MenuRepository : IMenuRepository
    {
        private readonly SapaFoRestRmsContext _context;

        public MenuRepository(SapaFoRestRmsContext context)
        {
            _context = context;
        }

        public Task CreateAsync(MenuItem entity)
        {
            throw new NotImplementedException();
        }

        public Task DeleteByIdAsync(int id)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<MenuItem>> GetAllAsync()
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<MenuItem>> GetAllMenus()
        {
            return await _context.MenuItems.Where(m => m.IsAvailable ==  true).Include(p => p.Category).ToListAsync();
        }

        public Task<MenuItem> GetByIdAsync(int id)
        {
            throw new NotImplementedException();
        }

        public Task UpdateAsync(MenuItem entity)
        {
            throw new NotImplementedException();
        }
    }
}
