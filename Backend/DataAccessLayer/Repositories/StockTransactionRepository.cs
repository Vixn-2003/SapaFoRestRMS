using DataAccessLayer.Dbcontext;
using DataAccessLayer.Repositories.Interfaces;
using DomainAccessLayer.Models;
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
    }
}
