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
        private static readonly string[] KitchenActiveStatuses = new[]
        {
            "Pending",
            "Cooking",
            "Ready",
            "Late",
            "Done"
        };

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
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.OrderComboItems)
                        .ThenInclude(oci => oci.MenuItem)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Combo)
                        .ThenInclude(c => c.ComboItems)
                            .ThenInclude(ci => ci.MenuItem)
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
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.OrderComboItems)
                        .ThenInclude(oci => oci.MenuItem)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Combo)
                        .ThenInclude(c => c.ComboItems)
                            .ThenInclude(ci => ci.MenuItem)
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
                            .ThenInclude(t => t.Area)
                // Bao gồm các trạng thái đang hoạt động trong bếp + Completed/Hoàn thành
                .Where(o => KitchenActiveStatuses.Contains(o.Status)
                            || o.Status == "Completed"
                            || o.Status == "Hoàn thành")
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
            // Lấy các orders có món bếp đã hoàn tất (Ready/Done)
            // Trạng thái đơn có thể là Completed hoặc vẫn đang active; bước lọc "đơn đã thực sự hoàn thành"
            // sẽ được xử lý ở tầng service dựa trên trạng thái từng món.
            return await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.MenuItem)
                        .ThenInclude(mi => mi.Category)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Combo)
                        .ThenInclude(c => c.ComboItems)
                            .ThenInclude(ci => ci.MenuItem)
                .Include(o => o.Customer)
                    .ThenInclude(c => c.User)
                .Include(o => o.Reservation)
                    .ThenInclude(r => r.Customer)
                        .ThenInclude(c => c.User)
                .Include(o => o.Reservation)
                    .ThenInclude(r => r.ReservationTables)
                        .ThenInclude(rt => rt.Table)
                            .ThenInclude(t => t.Area)
                .Where(o => o.OrderDetails.Any(od =>
                    od.Status == "Done" || od.Status == "Hoàn thành" ||
                    od.Status == "Ready" || od.Status == "Sẵn sàng"))
                // Không filter theo CreatedAt vì không có CompletedAt chính xác;
                // lấy tối đa 50 đơn gần nhất theo thời gian tạo để tránh quá nhiều dữ liệu
                .OrderByDescending(o => o.CreatedAt)
                .Take(50)
                .ToListAsync();
        }

        public async Task<List<Order>> GetActiveOrdersWithFullDetailsAsync()
        {
            return await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.MenuItem)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Combo)
                        .ThenInclude(c => c.ComboItems)
                            .ThenInclude(ci => ci.MenuItem)
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
                            .ThenInclude(t => t.Area)
                .Where(o => KitchenActiveStatuses.Contains(o.Status))
                .OrderBy(o => o.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Order>> GetActiveOrdersForGroupingAsync()
        {
            return await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.MenuItem)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.OrderComboItems)
                        .ThenInclude(oci => oci.MenuItem)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Combo)
                        .ThenInclude(c => c.ComboItems)
                            .ThenInclude(ci => ci.MenuItem)
                .Include(o => o.Customer)
                    .ThenInclude(c => c.User)
                .Include(o => o.Reservation)
                    .ThenInclude(r => r.Customer)
                        .ThenInclude(c => c.User)
                .Include(o => o.Reservation)
                    .ThenInclude(r => r.ReservationTables)
                        .ThenInclude(rt => rt.Table)
                .Where(o => KitchenActiveStatuses.Contains(o.Status))
                .ToListAsync();
        }

        public async Task<List<Order>> GetActiveOrdersForStationAsync()
        {
            return await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.MenuItem)
                        .ThenInclude(mi => mi.Category)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.OrderComboItems)
                        .ThenInclude(oci => oci.MenuItem)
                            .ThenInclude(mi => mi.Category)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Combo)
                        .ThenInclude(c => c.ComboItems)
                            .ThenInclude(ci => ci.MenuItem)
                .Include(o => o.Customer)
                    .ThenInclude(c => c.User)
                .Include(o => o.Reservation)
                    .ThenInclude(r => r.Customer)
                        .ThenInclude(c => c.User)
                .Include(o => o.Reservation)
                    .ThenInclude(r => r.ReservationTables)
                        .ThenInclude(rt => rt.Table)
                // Bao gồm các trạng thái đang hoạt động trong bếp + Completed/Hoàn thành
                .Where(o => KitchenActiveStatuses.Contains(o.Status)
                            || o.Status == "Completed"
                            || o.Status == "Hoàn thành")
                .ToListAsync();
        }
    }
}

