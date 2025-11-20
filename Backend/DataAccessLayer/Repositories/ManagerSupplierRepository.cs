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
    public class ManagerSupplierRepository : IManagerSupplierRepository
    {
        private readonly SapaFoRestRmsContext _context;

        public ManagerSupplierRepository(SapaFoRestRmsContext context)
        {
            _context = context;
        }

        public Task AddAsync(Supplier entity)
        {
            throw new NotImplementedException();
        }

        public Task DeleteAsync(int id)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<Supplier>> GetAllAsync()
        {
            return await _context.Suppliers
                    .Include(s => s.PurchaseOrders) 
                      .ThenInclude(po => po.PurchaseOrderDetails)
                      .ThenInclude(x => x.Ingredient)
                            .ToListAsync();
        }

        public Task<Supplier?> GetByIdAsync(int id)
        {
            throw new NotImplementedException();
        }

        public Task SaveChangesAsync()
        {
            throw new NotImplementedException();
        }

        public Task UpdateAsync(Supplier entity)
        {
            throw new NotImplementedException();
        }
    }
}
