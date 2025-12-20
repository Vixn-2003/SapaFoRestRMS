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
            try
            {
                // Include Role navigation property to avoid null reference
                var userRoles = await _context.Users
                    .Include(u => u.Role)
                    .Where(u => u.RoleId != null)
                    .GroupBy(u => u.Role != null ? u.Role.RoleName : "Unknown")
                    .Select(g => new { RoleName = g.Key ?? "Unknown", Count = g.Count() })
                    .ToListAsync();

                // Debug logging
                Console.WriteLine($"GetUsersByRoleAsync: Found {userRoles.Count} role groups");
                foreach (var role in userRoles)
                {
                    Console.WriteLine($"  - {role.RoleName}: {role.Count} users");
                }

                // Handle potential duplicate keys by summing counts
                var result = userRoles
                    .GroupBy(x => x.RoleName)
                    .ToDictionary(g => g.Key, g => g.Sum(x => x.Count));
                
                Console.WriteLine($"GetUsersByRoleAsync: Returning dictionary with {result.Count} roles");
                return result;
            }
            catch (Exception ex)
            {
                // Return empty dictionary as fallback
                Console.WriteLine($"Error in GetUsersByRoleAsync: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return new Dictionary<string, int>();
            }
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

        /// <summary>
        /// Đếm số payment đã hoàn thành hôm nay - Áp dụng logic từ OwnerRevenueService
        /// Chỉ đếm transactions có Status = "Paid" và CompletedAt.HasValue
        /// </summary>
        public async Task<int> GetCompletedPaymentsTodayAsync()
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var todayStart = today.ToDateTime(TimeOnly.MinValue);
            var todayEnd = today.ToDateTime(TimeOnly.MaxValue);

            return await _context.Transactions
                .Where(t => t.Status == "Paid" && t.CompletedAt.HasValue)
                .Where(t => t.CompletedAt.Value >= todayStart && t.CompletedAt.Value <= todayEnd)
                .CountAsync();
        }

        public async Task<int> GetPendingPaymentsAsync()
        {
            return await _context.Transactions
                .Where(t => t.Status == "Pending" || t.Status == "Processing")
                .CountAsync();
        }

        // ========== REVENUE STATISTICS ==========
        /// <summary>
        /// Tính doanh thu hôm nay
        /// = Sum(Transaction.Amount) với Status = "Paid" và CompletedAt.Date = today
        ///   (loại bỏ Split Bill parent transactions và child transactions)
        /// + Sum(ReservationDeposit.Amount) với DepositDate.Date = today VÀ Reservation.Status = "Completed"
        /// </summary>
        public async Task<decimal> GetTodayRevenueAsync()
        {
            var today = DateTime.Today;
            var todayStart = today.Date;
            var todayEnd = today.Date;

            // Doanh thu từ Transactions (loại bỏ Split Bill parent và child transactions)
            var transactionRevenue = await _context.Transactions
                .Where(t => t.Status == "Paid" && t.CompletedAt.HasValue)
                .Where(t => t.ParentTransactionId == null) // ✅ Loại bỏ child transactions
                .Where(t => t.PaymentMethod != "Split") // ✅ Loại bỏ parent Split transactions
                .Where(t => t.CompletedAt.Value.Date >= todayStart && t.CompletedAt.Value.Date <= todayEnd)
                .SumAsync(t => (decimal?)t.Amount) ?? 0m;

            // Doanh thu từ ReservationDeposits (tiền cọc) - CHỈ tính từ Reservation có Status = "Completed"
            var depositRevenue = await _context.ReservationDeposits
                .Include(d => d.Reservation)
                .Where(d => d.DepositDate.Date >= todayStart && d.DepositDate.Date <= todayEnd)
                .Where(d => d.Reservation != null && d.Reservation.Status == "Completed") // ✅ Chỉ tính từ Completed reservations
                .SumAsync(d => (decimal?)d.Amount) ?? 0m;

            return transactionRevenue + depositRevenue;
        }

        /// <summary>
        /// Tính doanh thu 7 ngày gần nhất - Áp dụng logic từ OwnerRevenueService
        /// Chỉ lấy transactions có Status = "Paid" và CompletedAt.HasValue
        /// (loại bỏ Split Bill parent transactions và child transactions)
        /// </summary>
        public async Task<List<(DateTime Date, decimal Revenue)>> GetRevenueLast7DaysAsync()
        {
            var startDate = DateTime.Today.AddDays(-6); // 7 days including today
            var endDate = DateTime.Today;

            var transactions = await _context.Transactions
                .Where(t => t.Status == "Paid" && t.CompletedAt.HasValue)
                .Where(t => t.ParentTransactionId == null) // ✅ Loại bỏ child transactions
                .Where(t => t.PaymentMethod != "Split") // ✅ Loại bỏ parent Split transactions
                .Where(t => t.CompletedAt.Value.Date >= startDate.Date && t.CompletedAt.Value.Date <= endDate.Date)
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

        /// <summary>
        /// Tính doanh thu tháng hiện tại - Áp dụng logic từ OwnerRevenueService
        /// Chỉ lấy transactions có Status = "Paid" và CompletedAt.HasValue
        /// (loại bỏ Split Bill parent transactions và child transactions)
        /// </summary>
        public async Task<decimal> GetMonthRevenueAsync()
        {
            var firstDayOfMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

            return await _context.Transactions
                .Where(t => t.Status == "Paid" && t.CompletedAt.HasValue)
                .Where(t => t.ParentTransactionId == null) // ✅ Loại bỏ child transactions
                .Where(t => t.PaymentMethod != "Split") // ✅ Loại bỏ parent Split transactions
                .Where(t => t.CompletedAt.Value.Date >= firstDayOfMonth.Date && t.CompletedAt.Value.Date <= lastDayOfMonth.Date)
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
            try
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
                    var totalAvailable = ingredient.InventoryBatches?.Sum(b => b.QuantityRemaining - b.QuantityReserved) ?? 0;

                    // Nếu available <= ReorderLevel thì coi là low stock
                    if (totalAvailable <= ingredient.ReorderLevel.Value)
                    {
                        lowStockCount++;
                    }
                }

                return lowStockCount;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetLowStockCountAsync: {ex.Message}");
                return 0;
            }
        }

        public async Task<int> GetExpiredIngredientsCountAsync()
        {
            try
            {
                var today = DateOnly.FromDateTime(DateTime.Today);

                return await _context.InventoryBatches
                    .Where(b => b.ExpiryDate.HasValue && b.ExpiryDate.Value < today)
                    .Where(b => b.QuantityRemaining > 0) // Còn tồn kho
                    .CountAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetExpiredIngredientsCountAsync: {ex.Message}");
                return 0;
            }
        }

        public async Task<int> GetNearExpiryIngredientsCountAsync()
        {
            try
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
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetNearExpiryIngredientsCountAsync: {ex.Message}");
                return 0;
            }
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

        /// <summary>
        /// Lấy top 5 danh mục bán chạy nhất - Áp dụng logic từ OwnerRevenueService
        /// Chỉ lấy orders có Status = "Paid" để đồng nhất với cách tính revenue
        /// </summary>
        public async Task<List<(string CategoryName, int ItemsSold, decimal Revenue)>> GetTop5BestSellingCategoriesAsync()
        {
            // Lấy dữ liệu từ OrderDetails join với MenuItem và Category
            // Chỉ lấy orders có Status = "Paid" để đồng nhất với cách tính revenue từ Transactions
            var categoryStats = await _context.OrderDetails
                .Where(od => od.Order.Status == "Paid")
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

