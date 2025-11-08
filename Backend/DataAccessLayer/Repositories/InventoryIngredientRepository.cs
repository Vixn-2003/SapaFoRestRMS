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

        public Task AddAsync(Ingredient entity)
        {
            throw new NotImplementedException();
        }

        public Task DeleteAsync(int id)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<Ingredient>> GetAllAsync()
        {
            return await _context.Ingredients
                .Include(i => i.InventoryBatches)
                    .ThenInclude(b => b.StockTransactions)                    
                .ToListAsync();
        }

        public async Task<(decimal totalImport, decimal totalExport, decimal totalFirst)> GetTotalImportExportBatches( int BatchesId, DateTime? startDate, DateTime? endDate)
        {
            if (endDate == null)
            {
                endDate = DateTime.Now; 
            }

            if (startDate == null)
            {
                startDate = endDate.Value.AddDays(-7);
            }

            var transactions = await _context.StockTransactions
                .Where(t => t.BatchId == BatchesId
                            && t.TransactionDate >= startDate
                            && t.TransactionDate <= endDate)
                .ToListAsync();

            decimal totalImport = transactions
                .Where(t => t.Type == "Import")
                .Sum(t => t.Quantity);

            decimal totalExport = transactions
                .Where(t => t.Type == "Export")
                .Sum(t => t.Quantity);

            var transactionExist = await _context.StockTransactions
                .Where(t => t.BatchId == BatchesId
                            && t.TransactionDate <= startDate)
                .ToListAsync();

            decimal totalImportE = transactionExist
                .Where(t => t.Type == "Import")
                .Sum(t => t.Quantity);

            decimal totalExportE = transactionExist
                .Where(t => t.Type == "Export")
                .Sum(t => t.Quantity);

            decimal totalFirst = totalImportE - totalExportE;

            return (totalImport, totalExport, totalFirst);
        }


        public Task<Ingredient?> GetByIdAsync(int id)
        {
            throw new NotImplementedException();
        }

        public Task SaveChangesAsync()
        {
            throw new NotImplementedException();
        }

        public Task UpdateAsync(Ingredient entity)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<InventoryBatch>> getBatchById(int id)
        {
            return await _context.InventoryBatches.Include(x => x.Warehouse)
                .Include(i => i.Ingredient)                
                .Include(i => i.PurchaseOrderDetail)
                    .ThenInclude(p => p.PurchaseOrder)
                        .ThenInclude(o => o.Supplier)
                .Where(i => i.IngredientId == id)
                .ToListAsync();
        }

        public async Task<bool> UpdateBatchWarehouse(int idBatch, int idWarehouse)
        {
            var batch = await _context.InventoryBatches
                .FirstOrDefaultAsync(b => b.BatchId == idBatch);

            if (batch == null)
                return false;


            batch.WarehouseId = idWarehouse;

            await _context.SaveChangesAsync();

            return true;
        }

    }
}
