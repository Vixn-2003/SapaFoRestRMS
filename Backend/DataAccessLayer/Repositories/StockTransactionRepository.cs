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
    public class StockTransactionRepository : IStockTransactionRepository
    {
        private readonly SapaFoRestRmsContext _context;

        public StockTransactionRepository(SapaFoRestRmsContext context)
        {
            _context = context;
        }
        public async Task<bool> AddNewStockTransaction(StockTransaction stockTransaction)
        {
            await _context.StockTransactions.AddAsync(stockTransaction);
            // Don't save here - let UnitOfWork.SaveChangesAsync() handle it
            return true;
        }

        public async Task<IEnumerable<StockTransaction>> GetExportTransactionsAsync()
        {
            return await _context.StockTransactions
                .Include(st => st.Ingredient)
                    .ThenInclude(i => i.Unit)
                .Include(st => st.Batch)
                    .ThenInclude(b => b.Warehouse)
                .Include(st => st.Batch)
                    .ThenInclude(b => b.PurchaseOrderDetail)
                        .ThenInclude(pod => pod.PurchaseOrder)
                            .ThenInclude(po => po.Supplier)
                .Where(st => st.Type == "Export")
                .OrderByDescending(st => st.TransactionDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<StockTransaction>> GetAllExport()
        {
            return await _context.StockTransactions
        .Where(p => p.Type == "Export"
                 && p.Batch != null                
                 && p.Batch.Ingredient != null        
                 && p.Batch.Warehouse != null         
                 && p.Batch.Ingredient.Unit != null)   
        .Include(t => t.Batch)
            .ThenInclude(b => b.Ingredient)
                .ThenInclude(i => i.Unit)
        .Include(t => t.Batch)
            .ThenInclude(b => b.Warehouse)
        .Include(t => t.Batch)
            .ThenInclude(b => b.PurchaseOrderDetail)
                .ThenInclude(pod => pod.PurchaseOrder)
                    .ThenInclude(po => po.Supplier)
        .ToListAsync();
        }

        public async Task<StockTransaction?> GetByIdAsync(int transactionId)
        {
            return await _context.StockTransactions
                .FirstOrDefaultAsync(x => x.TransactionId == transactionId);
        }

       
    }
}
