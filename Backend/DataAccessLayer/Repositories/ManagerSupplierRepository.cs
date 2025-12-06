using DataAccessLayer.Dbcontext;
using DataAccessLayer.Repositories.Interfaces;
using DomainAccessLayer.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories
{
    public class ManagerSupplierRepository : IManagerSupplierRepository
    {
        private readonly SapaFoRestRmsContext _context;

        public ManagerSupplierRepository(SapaFoRestRmsContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Supplier>> GetAllAsync()
        {
            return await _context.Suppliers
                .Where(x => x.IsActive == true)
                .Include(s => s.PurchaseOrders)
                    .ThenInclude(po => po.PurchaseOrderDetails)
                    .ThenInclude(x => x.Ingredient)
                .ToListAsync();
        }

        public async Task<Supplier?> GetByIdAsync(int id)
        {
            return await _context.Suppliers
                .Where(s => s.SupplierId == id && s.IsActive == true)
                .FirstOrDefaultAsync();
        }

        // ✅ THÊM METHOD MỚI
        public async Task<Supplier?> GetByCodeAsync(string code)
        {
            return await _context.Suppliers
                .Where(s => s.CodeSupplier == code && s.IsActive == true)
                .FirstOrDefaultAsync();
        }

        // ✅ THÊM METHOD MỚI
        public async Task<bool> CheckCodeExistsAsync(string code)
        {
            return await _context.Suppliers
                .AnyAsync(s => s.CodeSupplier == code && s.IsActive == true);
        }

        // ✅ IMPLEMENT METHOD
        public async Task AddAsync(Supplier entity)
        {
            entity.IsActive = true;
            await _context.Suppliers.AddAsync(entity);
        }

        // ✅ IMPLEMENT METHOD
        public async Task UpdateAsync(Supplier entity)
        {
            _context.Suppliers.Update(entity);
            await Task.CompletedTask;
        }

        // ✅ IMPLEMENT METHOD (Soft Delete)
        public async Task DeleteAsync(int id)
        {
            var supplier = await GetByIdAsync(id);
            if (supplier != null)
            {
                supplier.IsActive = false;
                _context.Suppliers.Update(supplier);
            }
        }

        // ✅ IMPLEMENT METHOD
        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}