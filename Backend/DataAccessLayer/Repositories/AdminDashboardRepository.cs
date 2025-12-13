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
    /// Repository implementation cho Admin Dashboard
    /// </summary>
    public class AdminDashboardRepository : IAdminDashboardRepository
    {
        private readonly SapaFoRestRmsContext _context;

        public AdminDashboardRepository(SapaFoRestRmsContext context)
        {
            _context = context;
        }

        // ========== USER STATISTICS ==========
        public async Task<int> GetTotalUsersAsync()
        {
            return await _context.Users.CountAsync();
        }

        public async Task<int> GetActiveUsersAsync()
        {
            return await _context.Users
                .Where(u => u.Status == 0) // Status = 0 means Active
                .CountAsync();
        }

        public async Task<int> GetInactiveUsersAsync()
        {
            return await _context.Users
                .Where(u => u.Status != 0) // Status != 0 means Inactive
                .CountAsync();
        }

        public async Task<Dictionary<string, int>> GetUsersByRoleAsync()
        {
            var userRoles = await _context.Users
                .Where(u => u.RoleId != null)
                .GroupBy(u => u.Role.RoleName)
                .Select(g => new { RoleName = g.Key, Count = g.Count() })
                .ToListAsync();

            return userRoles.ToDictionary(x => x.RoleName ?? "Unknown", x => x.Count);
        }

        // ========== SYSTEM ACTIVITY ==========
        public async Task<int> GetTodayReservationsAsync()
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var todayStart = today.ToDateTime(TimeOnly.MinValue);
            var todayEnd = today.ToDateTime(TimeOnly.MaxValue);

            return await _context.Reservations
                .Where(r => r.ReservationDate >= todayStart && r.ReservationDate <= todayEnd)
                .Where(r => r.Status != "Cancelled")
                .CountAsync();
        }

        public async Task<int> GetTodayOrdersAsync()
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var todayStart = today.ToDateTime(TimeOnly.MinValue);
            var todayEnd = today.ToDateTime(TimeOnly.MaxValue);

            return await _context.Orders
                .Where(o => o.CreatedAt.HasValue &&
                            o.CreatedAt.Value >= todayStart &&
                            o.CreatedAt.Value <= todayEnd)
                .CountAsync();
        }

        public async Task<int> GetCompletedPaymentsTodayAsync()
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

        public async Task<int> GetPendingPaymentsAsync()
        {
            return await _context.Transactions
                .Where(t => t.Status == "Pending" || t.Status == "Processing")
                .CountAsync();
        }

        // ========== REVENUE STATISTICS ==========
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

        public async Task<List<(DateTime Date, decimal Revenue)>> GetRevenueLast7DaysAsync()
        {
            var startDate = DateTime.Today.AddDays(-6); // 7 days including today
            var endDate = DateTime.Today.AddDays(1).AddSeconds(-1);

            var transactions = await _context.Transactions
                .Where(t => t.CompletedAt.HasValue &&
                            t.CompletedAt.Value >= startDate &&
                            t.CompletedAt.Value <= endDate)
                .Where(t => t.Status == "Paid" || t.Status == "Completed")
                .Select(t => new
                {
                    Date = t.CompletedAt!.Value.Date,
                    Amount = t.Amount
                })
                .ToListAsync();

            // Group by date and sum revenue
            var dailyRevenue = transactions
                .GroupBy(t => t.Date)
                .Select(g => (Date: g.Key, Revenue: g.Sum(x => x.Amount)))
                .ToList();

            // Fill missing dates with 0 revenue
            var result = new List<(DateTime Date, decimal Revenue)>();
            for (int i = 0; i < 7; i++)
            {
                var date = DateTime.Today.AddDays(-6 + i);
                var revenue = dailyRevenue.FirstOrDefault(x => x.Date == date).Revenue;
                result.Add((date, revenue));
            }

            return result;
        }

        public async Task<decimal> GetMonthRevenueAsync()
        {
            var firstDayOfMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddSeconds(-1);

            return await _context.Transactions
                .Where(t => t.CompletedAt.HasValue &&
                            t.CompletedAt.Value >= firstDayOfMonth &&
                            t.CompletedAt.Value <= lastDayOfMonth)
                .Where(t => t.Status == "Paid" || t.Status == "Completed")
                .SumAsync(t => (decimal?)t.Amount) ?? 0m;
        }

        // ========== ORDER STATISTICS ==========
        public async Task<List<(DateTime Date, int OrderCount)>> GetOrdersLast7DaysAsync()
        {
            var startDate = DateTime.Today.AddDays(-6); // 7 days including today
            var endDate = DateTime.Today.AddDays(1).AddSeconds(-1);

            var orders = await _context.Orders
                .Where(o => o.CreatedAt.HasValue &&
                            o.CreatedAt.Value >= startDate &&
                            o.CreatedAt.Value <= endDate)
                .Select(o => new
                {
                    Date = o.CreatedAt!.Value.Date
                })
                .ToListAsync();

            // Group by date and count orders
            var dailyOrders = orders
                .GroupBy(o => o.Date)
                .Select(g => (Date: g.Key, OrderCount: g.Count()))
                .ToList();

            // Fill missing dates with 0 orders
            var result = new List<(DateTime Date, int OrderCount)>();
            for (int i = 0; i < 7; i++)
            {
                var date = DateTime.Today.AddDays(-6 + i);
                var orderCount = dailyOrders.FirstOrDefault(x => x.Date == date).OrderCount;
                result.Add((date, orderCount));
            }

            return result;
        }

        // ========== WAREHOUSE ALERTS ==========
        public async Task<int> GetLowStockCountAsync()
        {
            // Lấy tất cả ingredients có ReorderLevel
            var ingredients = await _context.Ingredients
                .Include(i => i.InventoryBatches)
                .Where(i => i.ReorderLevel.HasValue && i.ReorderLevel.Value > 0)
                .ToListAsync();

            // Đếm số ingredients có tổng available < ReorderLevel
            int lowStockCount = 0;
            foreach (var ingredient in ingredients)
            {
                // Tính tổng available = tổng QuantityRemaining - tổng QuantityReserved
                var totalAvailable = ingredient.InventoryBatches.Sum(b => b.QuantityRemaining - b.QuantityReserved);
                
                // Nếu available <= ReorderLevel thì coi là low stock
                if (totalAvailable <= ingredient.ReorderLevel.Value)
                {
                    lowStockCount++;
                }
            }

            return lowStockCount;
        }

        public async Task<int> GetExpiredIngredientsCountAsync()
        {
            var today = DateOnly.FromDateTime(DateTime.Today);

            return await _context.InventoryBatches
                .Where(b => b.ExpiryDate.HasValue && b.ExpiryDate.Value < today)
                .Where(b => b.QuantityRemaining > 0) // Còn tồn kho
                .CountAsync();
        }

        public async Task<int> GetNearExpiryIngredientsCountAsync()
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var sevenDaysLater = today.AddDays(7);

            return await _context.InventoryBatches
                .Where(b => b.ExpiryDate.HasValue &&
                            b.ExpiryDate.Value >= today &&
                            b.ExpiryDate.Value <= sevenDaysLater)
                .Where(b => b.QuantityRemaining > 0) // Còn tồn kho
                .CountAsync();
        }

        // ========== TOP ANALYTICS ==========
        public async Task<List<(int UserId, string Username, string FullName, int LoginCount)>> GetTop5ActiveUsersAsync()
        {
            // Track login activity from AuditLogs where EventType contains "login"
            var topUsers = await _context.AuditLogs
                .Where(a => a.UserId.HasValue && 
                            a.EventType != null && 
                            (a.EventType.ToLower().Contains("login") || a.EventType.ToLower().Contains("auth")))
                .Include(a => a.User)
                .GroupBy(a => new 
                { 
                    UserId = a.UserId.Value,
                    Username = a.User != null ? a.User.Email : "Unknown",
                    FullName = a.User != null ? a.User.FullName : "N/A"
                })
                .Select(g => new
                {
                    UserId = g.Key.UserId,
                    Username = g.Key.Username,
                    FullName = g.Key.FullName,
                    LoginCount = g.Count()
                })
                .OrderByDescending(x => x.LoginCount)
                .Take(5)
                .ToListAsync();

            var result = topUsers.Select(tu => (
                UserId: tu.UserId,
                Username: tu.Username,
                FullName: tu.FullName,
                LoginCount: tu.LoginCount
            )).ToList();

            return result;
        }

        public async Task<List<(string CategoryName, int ItemsSold, decimal Revenue)>> GetTop5BestSellingCategoriesAsync()
        {
            // Lấy dữ liệu từ OrderDetails join với MenuItem và Category
            var categoryStats = await _context.OrderDetails
                .Where(od => od.Order.Status == "Paid" || od.Order.Status == "Completed")
                .GroupBy(od => od.MenuItem.Category.CategoryName)
                .Select(g => new
                {
                    CategoryName = g.Key ?? "Uncategorized",
                    ItemsSold = g.Sum(od => od.Quantity),
                    Revenue = g.Sum(od => od.Quantity * od.UnitPrice)
                })
                .OrderByDescending(x => x.Revenue)
                .Take(5)
                .ToListAsync();

            return categoryStats.Select(cs => (cs.CategoryName, cs.ItemsSold, cs.Revenue)).ToList();
        }

        // ========== SYSTEM LOGS ==========
        public async Task<List<(DateTime Time, string Username, string Action)>> GetRecentSystemLogsAsync()
        {
            var logs = await _context.AuditLogs
                .Include(a => a.User)
                .OrderByDescending(a => a.CreatedAt)
                .Take(10)
                .Select(a => new
                {
                    Time = a.CreatedAt,
                    Username = a.User != null ? a.User.Email : "System",
                    Action = a.EventType + (a.Description != null ? ": " + a.Description : "")
                })
                .ToListAsync();

            return logs.Select(l => (l.Time, l.Username, l.Action)).ToList();
        }
    }
}

