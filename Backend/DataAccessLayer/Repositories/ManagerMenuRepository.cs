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

        public async Task UpdateAsync(MenuItem entity)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> ManagerUpdateMenu(MenuItem menuItem)
        {
            var existingItem = await _context.MenuItems
                .FirstOrDefaultAsync(x => x.MenuItemId == menuItem.MenuItemId);

            if (existingItem == null)
                return false; // Không tìm thấy item để cập nhật

            // Cập nhật các thuộc tính
            existingItem.Name = menuItem.Name;
            existingItem.CategoryId = menuItem.CategoryId;
            existingItem.Price = menuItem.Price;
            existingItem.IsAvailable = menuItem.IsAvailable;
            existingItem.CourseType = menuItem.CourseType;
            existingItem.Description = menuItem.Description;
            existingItem.ImageUrl = menuItem.ImageUrl;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<Recipe>> GetRecipeByMenuItem(int id)
        {
            return await _context.Recipes.Where(x => x.MenuItemId == id).ToListAsync();
        }

        public async Task<bool> DeleteRecipeByMenuItemId(int menuItemId)
        {
            var recipes = _context.Recipes.Where(r => r.MenuItemId == menuItemId);
            _context.Recipes.RemoveRange(recipes);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> AddRecipe(Recipe recipe)
        {
            var entity = new Recipe
            {
                MenuItemId = recipe.MenuItemId,
                IngredientId = recipe.IngredientId,
                QuantityNeeded = recipe.QuantityNeeded
            };
            _context.Recipes.Add(entity);
            await _context.SaveChangesAsync();
            return true;
        }

    }
}
