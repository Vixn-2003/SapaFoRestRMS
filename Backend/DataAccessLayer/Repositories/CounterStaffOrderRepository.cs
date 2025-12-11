using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataAccessLayer.Dbcontext;
using DataAccessLayer.Repositories.Interfaces;
using DomainAccessLayer.Models;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Repositories
{
    /// <summary>
    /// Repository implementation cho Counter Staff Order Management - UC123
    /// </summary>
    public class CounterStaffOrderRepository : ICounterStaffOrderRepository
    {
        private readonly SapaFoRestRmsContext _context;

        public CounterStaffOrderRepository(SapaFoRestRmsContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Order>> GetAllOrdersAsync(string? statusFilter = null, DateOnly? date = null)
        {
            var query = _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.MenuItem)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Combo)
                .Include(o => o.Customer)
                    .ThenInclude(c => c!.User)
                .Include(o => o.Reservation)
                    .ThenInclude(r => r!.ReservationTables)
                        .ThenInclude(rt => rt.Table)
                .Include(o => o.Reservation)
                    .ThenInclude(r => r!.Customer)
                        .ThenInclude(c => c!.User)
                .Include(o => o.ConfirmedByStaff)
                    .ThenInclude(s => s!.User)
                .AsQueryable();

            // Filter by date
            if (date.HasValue)
            {
                var dateStart = date.Value.ToDateTime(TimeOnly.MinValue);
                var dateEnd = date.Value.ToDateTime(TimeOnly.MaxValue);
                query = query.Where(o => o.CreatedAt.HasValue &&
                                         o.CreatedAt.Value >= dateStart &&
                                         o.CreatedAt.Value <= dateEnd);
            }

            // Filter by status
            if (!string.IsNullOrWhiteSpace(statusFilter))
            {
                query = query.Where(o => o.Status == statusFilter);
            }

            return await query
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }

        public async Task<Order?> GetOrderSummaryAsync(int orderId)
        {
            return await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.MenuItem)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Combo)
                .Include(o => o.Customer)
                    .ThenInclude(c => c!.User)
                .Include(o => o.Reservation)
                    .ThenInclude(r => r!.ReservationTables)
                        .ThenInclude(rt => rt.Table)
                .Include(o => o.Reservation)
                    .ThenInclude(r => r!.Customer)
                        .ThenInclude(c => c!.User)
                .Include(o => o.ConfirmedByStaff)
                    .ThenInclude(s => s!.User)
                .Include(o => o.Transactions)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);
        }
    }
}

