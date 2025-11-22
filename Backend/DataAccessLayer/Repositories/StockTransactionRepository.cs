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

        public async Task<IEnumerable<StockTransaction>> GetExportTransactionsAsync()
        {
            return await _context.StockTransactions
                .Include(st => st.Ingredient)
                .Include(st => st.Batch)
                .Where(st => st.Type == "Export")
                .OrderByDescending(st => st.TransactionDate)
                .ToListAsync();
        }
    }
}
