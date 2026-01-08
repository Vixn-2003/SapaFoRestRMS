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
    public class ManagerCategoryRepository : IManagerCategoryRepository
    {

        private readonly SapaFoRestRmsContext _context;

        public ManagerCategoryRepository(SapaFoRestRmsContext context)
        {
            _context = context;
        }
        public Task AddAsync(MenuCategory entity)
        {
            throw new NotImplementedException();
        }

        public Task DeleteAsync(int id)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<MenuCategory>> GetAllAsync()
        {
            throw new NotImplementedException();
        }

        public Task<MenuCategory?> GetByIdAsync(int id)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<MenuCategory>> GetManagerAllCategory()
        {
            return await _context.MenuCategories.ToListAsync();
        }

        public Task SaveChangesAsync()
        {
            throw new NotImplementedException();
        }

        public Task UpdateAsync(MenuCategory entity)
        {
            throw new NotImplementedException();
        }

        public async Task<List<string>> GetCategoryNamesAsync()
        {
            return await _context.MenuCategories
                .Select(c => c.CategoryName)
                .ToListAsync();
        }
    }
}
