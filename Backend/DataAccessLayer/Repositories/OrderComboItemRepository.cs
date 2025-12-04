using DataAccessLayer.Dbcontext;
using DataAccessLayer.Repositories.Interfaces;
using DomainAccessLayer.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories
{
    public class OrderComboItemRepository : IOrderComboItemRepository
    {
        private readonly SapaFoRestRmsContext _context;

        public OrderComboItemRepository(SapaFoRestRmsContext context)
        {
            _context = context;
        }

        public async Task<OrderComboItem?> GetByIdAsync(int id)
        {
            return await _context.OrderComboItems.FindAsync(id);
        }

        public async Task<IEnumerable<OrderComboItem>> GetAllAsync()
        {
            return await _context.OrderComboItems.ToListAsync();
        }

        public async Task AddAsync(OrderComboItem entity)
        {
            await _context.OrderComboItems.AddAsync(entity);
        }

        public async Task UpdateAsync(OrderComboItem entity)
        {
            _context.OrderComboItems.Update(entity);
            await Task.CompletedTask;
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await GetByIdAsync(id);
            if (entity != null)
            {
                _context.OrderComboItems.Remove(entity);
            }
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task<OrderComboItem?> GetByIdWithMenuItemAsync(int orderComboItemId)
        {
            return await _context.OrderComboItems
                .Include(oci => oci.MenuItem)
                .Include(oci => oci.OrderDetail)
                .FirstOrDefaultAsync(oci => oci.OrderComboItemId == orderComboItemId);
        }

        public async Task<List<OrderComboItem>> GetByOrderDetailIdAsync(int orderDetailId)
        {
            return await _context.OrderComboItems
                .Include(oci => oci.MenuItem)
                .Where(oci => oci.OrderDetailId == orderDetailId)
                .ToListAsync();
        }
    }
}

