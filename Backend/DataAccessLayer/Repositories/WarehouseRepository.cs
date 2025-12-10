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
    public class WarehouseRepository : IWarehouseRepository
    {
        private readonly SapaFoRestRmsContext _context;

        public WarehouseRepository(SapaFoRestRmsContext context)
        {
            _context = context;
        }
        public Task AddAsync(Warehouse entity)
        {
            throw new NotImplementedException();
        }

        public Task DeleteAsync(int id)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<Warehouse>> GetAllAsync()
        {
            return await _context.Warehouses.Where(x => x.IsActive == true).ToListAsync();
        }

        public async Task<Warehouse?> GetByIdAsync(int id)
        {
            return await _context.Warehouses
                .FirstOrDefaultAsync(x => x.IsActive && x.WarehouseId == id);
        }

        public async Task<int> GetIdByStringAsync(string warehouse)
        {
            if (string.IsNullOrWhiteSpace(warehouse))
                return 0;

            var unit = await _context.Warehouses
                .FirstOrDefaultAsync(u => u.Name == warehouse);

            return unit?.WarehouseId ?? 0;
        }

        public Task SaveChangesAsync()
        {
            throw new NotImplementedException();
        }

        public Task UpdateAsync(Warehouse entity)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<InventoryBatch>> GetBatchesByWarehouseIdAsync(int warehouseId)
        {
            return await _context.InventoryBatches
                .Include(b => b.Ingredient)
                    .ThenInclude(i => i.Unit)
                .Include(b => b.Warehouse)
                .Include(b => b.PurchaseOrderDetail)
                    .ThenInclude(pod => pod.PurchaseOrder)
                .Where(b => b.WarehouseId == warehouseId && b.IsActive)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();
        }
    }
}
