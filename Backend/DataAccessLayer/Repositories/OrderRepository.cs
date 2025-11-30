using DataAccessLayer.Dbcontext;
using DataAccessLayer.Repositories.Interfaces;
using DomainAccessLayer.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly SapaFoRestRmsContext _context;

        public OrderRepository(SapaFoRestRmsContext context)
        {
            _context = context;
        }

        public async Task<Order?> GetByIdAsync(int id)
        {
            return await _context.Orders.FindAsync(id);
        }

        public async Task<IEnumerable<Order>> GetAllAsync()
        {
            return await _context.Orders.ToListAsync();
        }

        public async Task AddAsync(Order entity)
        {
            await _context.Orders.AddAsync(entity);
        }

        public async Task UpdateAsync(Order entity)
        {
            _context.Orders.Update(entity);
            await Task.CompletedTask;
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await GetByIdAsync(id);
            if (entity != null)
            {
                _context.Orders.Remove(entity);
            }
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task<Order?> GetByIdWithDetailsAsync(int orderId)
        {
            return await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.MenuItem)
                .Include(o => o.Customer)
                    .ThenInclude(c => c.User)
                .Include(o => o.Reservation)
                    .ThenInclude(r => r.Customer)
                        .ThenInclude(c => c.User)
                .Include(o => o.Reservation)
                    .ThenInclude(r => r.Staff)
                .Include(o => o.Reservation)
                    .ThenInclude(r => r.ReservationTables)
                        .ThenInclude(rt => rt.Table)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);
        }

        public async Task<Order?> GetByIdWithOrderDetailsAsync(int orderId)
        {
            return await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);
        }

        public async Task<List<Order>> GetActiveOrdersAsync()
        {
            return await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.MenuItem)
                .Include(o => o.Customer)
                    .ThenInclude(c => c.User)
                .Include(o => o.Reservation)
                    .ThenInclude(r => r.Customer)
                        .ThenInclude(c => c.User)
                .Include(o => o.Reservation)
                    .ThenInclude(r => r.Staff)
                .Include(o => o.Reservation)
                    .ThenInclude(r => r.ReservationTables)
                        .ThenInclude(rt => rt.Table)
                .Where(o => o.Status == "Processing" || o.Status == "Preparing")
                .OrderBy(o => o.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Order>> GetOrdersByStatusAsync(string status)
        {
            return await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.MenuItem)
                .Where(o => o.Status == status)
                .ToListAsync();
        }

        public async Task<List<Order>> GetOrdersByCustomerIdAsync(int customerId)
        {
            return await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.MenuItem)
                .Where(o => o.CustomerId == customerId)
                .ToListAsync();
        }

        public async Task<List<Order>> GetRecentlyFulfilledOrdersAsync(int minutesAgo)
        {
            // Lấy tất cả orders có items Done
            // Vì OrderDetail không có CompletedAt field, ta không thể filter chính xác theo thời gian Done
            // Nên lấy tất cả orders có items Done, không filter theo CreatedAt của order
            // (vì order có thể được tạo từ lâu nhưng mới hoàn thành gần đây)
            return await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.MenuItem)
                        .ThenInclude(mi => mi.Category)
                .Include(o => o.Customer)
                    .ThenInclude(c => c.User)
                .Include(o => o.Reservation)
                    .ThenInclude(r => r.Customer)
                        .ThenInclude(c => c.User)
                .Include(o => o.Reservation)
                    .ThenInclude(r => r.ReservationTables)
                        .ThenInclude(rt => rt.Table)
                .Where(o => (o.Status == "Processing" || o.Status == "Preparing" || o.Status == "Completed"))
                .Where(o => o.OrderDetails.Any(od => od.Status == "Done" || od.Status == "Hoàn thành"))
                // Bỏ filter theo CreatedAt vì không chính xác (order có thể tạo từ lâu nhưng mới Done gần đây)
                // Chỉ lấy orders được tạo trong vòng 24 giờ để tránh lấy quá nhiều dữ liệu cũ
                .Where(o => o.CreatedAt >= System.DateTime.Now.AddHours(-24))
                .OrderByDescending(o => o.CreatedAt)
                .Take(50)
                .ToListAsync();
        }

        public async Task<List<Order>> GetActiveOrdersWithFullDetailsAsync()
        {
            return await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.MenuItem)
                .Include(o => o.Customer)
                    .ThenInclude(c => c.User)
                .Include(o => o.Reservation)
                    .ThenInclude(r => r.Customer)
                        .ThenInclude(c => c.User)
                .Include(o => o.Reservation)
                    .ThenInclude(r => r.Staff)
                .Include(o => o.Reservation)
                    .ThenInclude(r => r.ReservationTables)
                        .ThenInclude(rt => rt.Table)
                .Where(o => o.Status == "Processing" || o.Status == "Preparing")
                .OrderBy(o => o.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Order>> GetActiveOrdersForGroupingAsync()
        {
            return await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.MenuItem)
                .Include(o => o.Customer)
                    .ThenInclude(c => c.User)
                .Include(o => o.Reservation)
                    .ThenInclude(r => r.Customer)
                        .ThenInclude(c => c.User)
                .Include(o => o.Reservation)
                    .ThenInclude(r => r.ReservationTables)
                        .ThenInclude(rt => rt.Table)
                .Where(o => o.Status == "Processing" || o.Status == "Preparing")
                .ToListAsync();
        }

        public async Task<List<Order>> GetActiveOrdersForStationAsync()
        {
            return await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.MenuItem)
                        .ThenInclude(mi => mi.Category)
                .Include(o => o.Customer)
                    .ThenInclude(c => c.User)
                .Include(o => o.Reservation)
                    .ThenInclude(r => r.Customer)
                        .ThenInclude(c => c.User)
                .Include(o => o.Reservation)
                    .ThenInclude(r => r.ReservationTables)
                        .ThenInclude(rt => rt.Table)
                .Where(o => o.Status == "Processing" || o.Status == "Preparing")
                .ToListAsync();
        }
    }
}

