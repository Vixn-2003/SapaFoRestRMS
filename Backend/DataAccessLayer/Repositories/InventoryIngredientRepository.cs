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
    public class InventoryIngredientRepository : IInventoryIngredientRepository
    {
        private readonly SapaFoRestRmsContext _context;

        public InventoryIngredientRepository(SapaFoRestRmsContext context)
        {
            _context = context;
        }
        public Task AddAsync(InventoryBatch entity)
        {
            throw new NotImplementedException();
        }

        public Task DeleteAsync(int id)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<InventoryBatch>> GetAllAsync()
        {
            return await _context.InventoryBatches
               .Include(b => b.Ingredient)
               .ToListAsync();
        }

        public Task<Ingredient?> GetByIdAsync(int id)
        {
            throw new NotImplementedException();
        }

        public Task SaveChangesAsync()
        {
            throw new NotImplementedException();
        }

        public Task UpdateAsync(InventoryBatch entity)
        {
            throw new NotImplementedException();
        }

        Task<InventoryBatch?> IRepository<InventoryBatch>.GetByIdAsync(int id)
        {
            throw new NotImplementedException();
        }
    }
}
