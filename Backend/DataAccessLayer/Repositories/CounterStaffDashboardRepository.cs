using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataAccessLayer.Dbcontext;
using DataAccessLayer.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Repositories
{
    /// <summary>
    /// Repository implementation cho Counter Staff Dashboard - UC122
    /// </summary>
    public class CounterStaffDashboardRepository : ICounterStaffDashboardRepository
    {
        private readonly SapaFoRestRmsContext _context;

        public CounterStaffDashboardRepository(SapaFoRestRmsContext context)
        {
            _context = context;
        }

        public async Task<int> GetTodayReservationCountAsync()
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var todayStart = today.ToDateTime(TimeOnly.MinValue);
            var todayEnd = today.ToDateTime(TimeOnly.MaxValue);

            return await _context.Reservations
                .Where(r => r.ReservationDate >= todayStart && r.ReservationDate <= todayEnd)
                .Where(r => r.Status != "Cancelled")
                .CountAsync();
        }

        public async Task<decimal> GetTodayRevenueAsync()
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var todayStart = today.ToDateTime(TimeOnly.MinValue);
            var todayEnd = today.ToDateTime(TimeOnly.MaxValue);

            return await _context.Transactions
                .Where(t => t.CompletedAt.HasValue &&
                            t.CompletedAt.Value >= todayStart &&
                            t.CompletedAt.Value <= todayEnd)
                .Where(t => t.Status == "Paid" || t.Status == "Completed")
                .SumAsync(t => (decimal?)t.Amount) ?? 0m;
        }

        public async Task<int> GetActiveOrdersCountAsync()
        {
            return await _context.Orders
                .Where(o => o.Status == "Confirmed" || o.Status == "Pending")
                .CountAsync();
        }

        public async Task<int> GetPendingPaymentOrdersAsync()
        {
            return await _context.Orders
                .Where(o => o.Status == "Confirmed")
                .Where(o => o.ConfirmedAt.HasValue) // Waiter đã xác nhận
                .CountAsync();
        }

        public async Task<int> GetActiveTablesCountAsync()
        {
            return await _context.Tables
                .Where(t => t.Status == "Occupied" || t.Status == "Reserved")
                .CountAsync();
        }

        public async Task<int> GetTransactionCountAsync()
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var todayStart = today.ToDateTime(TimeOnly.MinValue);
            var todayEnd = today.ToDateTime(TimeOnly.MaxValue);

            return await _context.Transactions
                .Where(t => t.CompletedAt.HasValue &&
                            t.CompletedAt.Value >= todayStart &&
                            t.CompletedAt.Value <= todayEnd)
                .Where(t => t.Status == "Paid" || t.Status == "Completed")
                .CountAsync();
        }

        public async Task<Dictionary<int, decimal>> GetHourlyRevenueChartAsync()
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var todayStart = today.ToDateTime(TimeOnly.MinValue);
            var todayEnd = today.ToDateTime(TimeOnly.MaxValue);

            var transactions = await _context.Transactions
                .Where(t => t.CompletedAt.HasValue &&
                            t.CompletedAt.Value >= todayStart &&
                            t.CompletedAt.Value <= todayEnd)
                .Where(t => t.Status == "Paid" || t.Status == "Completed")
                .Select(t => new
                {
                    Hour = t.CompletedAt!.Value.Hour,
                    Amount = t.Amount
                })
                .ToListAsync();

            // Group by hour and sum revenue
            var hourlyRevenue = transactions
                .GroupBy(t => t.Hour)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));

            // Fill missing hours with 0
            var result = new Dictionary<int, decimal>();
            for (int hour = 0; hour < 24; hour++)
            {
                result[hour] = hourlyRevenue.ContainsKey(hour) ? hourlyRevenue[hour] : 0m;
            }

            return result;
        }

        public async Task<Dictionary<int, int>> GetHourlyOrdersChartAsync()
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var todayStart = today.ToDateTime(TimeOnly.MinValue);
            var todayEnd = today.ToDateTime(TimeOnly.MaxValue);

            var orders = await _context.Orders
                .Where(o => o.CreatedAt.HasValue &&
                            o.CreatedAt.Value >= todayStart &&
                            o.CreatedAt.Value <= todayEnd)
                .Where(o => o.Status == "Paid" || o.Status == "Confirmed")
                .Select(o => new
                {
                    Hour = o.CreatedAt!.Value.Hour
                })
                .ToListAsync();

            // Group by hour and count
            var hourlyOrders = orders
                .GroupBy(o => o.Hour)
                .ToDictionary(g => g.Key, g => g.Count());

            // Fill missing hours with 0
            var result = new Dictionary<int, int>();
            for (int hour = 0; hour < 24; hour++)
            {
                result[hour] = hourlyOrders.ContainsKey(hour) ? hourlyOrders[hour] : 0;
            }

            return result;
        }
    }
}

