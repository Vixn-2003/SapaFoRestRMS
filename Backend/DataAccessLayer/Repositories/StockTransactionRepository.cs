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
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<IEnumerable<StockTransaction>> GetAllExport()
        {
            return await _context.StockTransactions
                .Where(p => p.Type == "Export")
                .Include(t => t.Batch)
                    .ThenInclude(b => b.Ingredient)
                        .ThenInclude(i => i.Unit)
                .Include(t => t.Batch.Warehouse)
                .Include(t => t.Batch.PurchaseOrderDetail)
                    .ThenInclude(pod => pod.PurchaseOrder)
                        .ThenInclude(po => po.Supplier)
                .ToListAsync();
        }


    }
}
