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
    public class MenuItemRepository : IMenuItemRepository
    {
        private readonly SapaFoRestRmsContext _context;

        public MenuItemRepository(SapaFoRestRmsContext context)
        {
            _context = context;
        }

        public Task AddAsync(MenuItem entity)
        {
            throw new NotImplementedException();
        }

        public Task DeleteAsync(int id)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<MenuItem>> GetAllAsync()
        {
            throw new NotImplementedException();
        }

        public Task<MenuItem?> GetByIdAsync(int id)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<MenuItem>> GetTopBestSellersAsync()
        {
            return await _context.MenuItems
                .Where(m => m.IsAds == true && m.IsAvailable == true)
                .Include(m => m.Category)
                .OrderBy(m => m.Name) // Sắp xếp theo tên
                .ToListAsync();
        }

        public Task SaveChangesAsync()
        {
            throw new NotImplementedException();
        }

        public Task UpdateAsync(MenuItem entity)
        {
            throw new NotImplementedException();
        }
    }
}