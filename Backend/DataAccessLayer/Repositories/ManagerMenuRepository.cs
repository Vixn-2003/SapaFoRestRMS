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
    public class ManagerMenuRepository : IManagerMenuRepository
    {
        private readonly SapaFoRestRmsContext _context;

        public ManagerMenuRepository(SapaFoRestRmsContext context)
        {
            _context = context;
        }

        public Task AddAsync(MenuItem entity)
        {
            throw new NotImplementedException();
        }

        public Task CreateAsync(MenuItem entity)
        {
            throw new NotImplementedException();
        }

        public Task DeleteAsync(int id)
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
        public async Task<IEnumerable<MenuItem>> GetManagerAllMenus()
        {
            return await _context.MenuItems.Where(m => m.IsAvailable == true).Include(p => p.Category).ToListAsync();
        }

        public async Task<MenuItem> ManagerMenuByIds(int id)
        {
            return await _context.MenuItems
                .Include(m => m.Category)                     // Lấy danh mục món
                .Include(m => m.Recipes)                      // Lấy danh sách Recipe của món
                    .ThenInclude(r => r.Ingredient)           // Lấy chi tiết Ingredient trong từng Recipe
                .Where(m => m.IsAvailable == true && m.MenuItemId == id)
                .FirstOrDefaultAsync();
        }



        public Task<MenuItem> GetByIdAsync(int id)
        {
            throw new NotImplementedException();
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
