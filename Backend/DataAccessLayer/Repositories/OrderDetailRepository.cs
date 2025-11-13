using DataAccessLayer.Dbcontext;
using DataAccessLayer.Repositories.Interfaces;
using DomainAccessLayer.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories
{
    public class OrderDetailRepository : IOrderDetailRepository
    {
        private readonly SapaFoRestRmsContext _context;

        public OrderDetailRepository(SapaFoRestRmsContext context)
        {
            _context = context;
        }

        public async Task<OrderDetail?> GetByIdAsync(int id)
        {
            return await _context.OrderDetails.FindAsync(id);
        }

        public async Task<IEnumerable<OrderDetail>> GetAllAsync()
        {
            return await _context.OrderDetails.ToListAsync();
        }

        public async Task AddAsync(OrderDetail entity)
        {
            await _context.OrderDetails.AddAsync(entity);
        }

        public async Task UpdateAsync(OrderDetail entity)
        {
            _context.OrderDetails.Update(entity);
            await Task.CompletedTask;
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await GetByIdAsync(id);
            if (entity != null)
            {
                _context.OrderDetails.Remove(entity);
            }
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task<OrderDetail?> GetByIdWithMenuItemAsync(int orderDetailId)
        {
            return await _context.OrderDetails
                .Include(od => od.MenuItem)
                .FirstOrDefaultAsync(od => od.OrderDetailId == orderDetailId);
        }

        public async Task<List<OrderDetail>> GetByOrderIdAsync(int orderId)
        {
            return await _context.OrderDetails
                .Include(od => od.MenuItem)
                .Where(od => od.OrderId == orderId)
                .ToListAsync();
        }

        public async Task<List<OrderDetail>> GetByOrderIdsAsync(List<int> orderIds)
        {
            return await _context.OrderDetails
                .Include(od => od.MenuItem)
                .Where(od => orderIds.Contains(od.OrderId))
                .ToListAsync();
        }
    }
}

