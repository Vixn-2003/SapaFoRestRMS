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

        /// <summary>
        /// Tính doanh thu hôm nay
        /// = Sum(Transaction.Amount) với Status = "Paid" và CompletedAt.Date = today
        ///   (loại bỏ Split Bill parent transactions và child transactions)
        /// + Sum(ReservationDeposit.Amount) với DepositDate.Date = today VÀ Reservation.Status = "Completed"
        /// </summary>
        public async Task<decimal> GetTodayRevenueAsync()
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var todayStart = today.ToDateTime(TimeOnly.MinValue);
            var todayEnd = today.ToDateTime(TimeOnly.MaxValue);

            // Doanh thu từ Transactions (loại bỏ Split Bill parent và child transactions)
            var transactionRevenue = await _context.Transactions
                .Where(t => t.Status == "Paid" && t.CompletedAt.HasValue)
                .Where(t => t.ParentTransactionId == null) // ✅ Loại bỏ child transactions
                .Where(t => t.PaymentMethod != "Split") // ✅ Loại bỏ parent Split transactions
                .Where(t => t.CompletedAt.Value.Date >= todayStart.Date && 
                            t.CompletedAt.Value.Date <= todayEnd.Date)
                .SumAsync(t => (decimal?)t.Amount) ?? 0m;

            // Doanh thu từ ReservationDeposits (tiền cọc) - CHỈ tính từ Reservation có Status = "Completed"
            var depositRevenue = await _context.ReservationDeposits
                .Include(d => d.Reservation)
                .Where(d => d.DepositDate.Date >= todayStart.Date && 
                           d.DepositDate.Date <= todayEnd.Date &&
                           d.Reservation != null &&
                           d.Reservation.Status == "Completed")
                .SumAsync(d => (decimal?)d.Amount) ?? 0m;

            return transactionRevenue + depositRevenue;
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

        /// <summary>
        /// Đếm số bàn đang sử dụng
        /// = Số bàn có Reservation với status "Guest Seated" (khách đã ngồi vào bàn)
        /// Lưu ý: Table.Status chỉ là trạng thái bàn (hỏng/dùng được), không phải trạng thái sử dụng
        /// </summary>
        public async Task<int> GetActiveTablesCountAsync()
        {
            // Đếm số bàn có Reservation với status "Guest Seated" (khách đã ngồi vào bàn)
            // Reservation phải có ReservationTables (bàn đã được gán)
            return await _context.ReservationTables
                .Include(rt => rt.Reservation)
                .Where(rt => rt.Reservation != null && 
                            rt.Reservation.Status == "Guest Seated")
                .Select(rt => rt.TableId)
                .Distinct()
                .CountAsync();
        }

        /// <summary>
        /// Đếm số transaction đã hoàn thành hôm nay - Áp dụng logic từ OwnerRevenueService
        /// Chỉ đếm transactions có Status = "Paid" và CompletedAt.HasValue
        /// </summary>
        public async Task<int> GetTransactionCountAsync()
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var todayStart = today.ToDateTime(TimeOnly.MinValue);
            var todayEnd = today.ToDateTime(TimeOnly.MaxValue);

            return await _context.Transactions
                .Where(t => t.Status == "Paid" && t.CompletedAt.HasValue)
                .Where(t => t.CompletedAt.Value >= todayStart && t.CompletedAt.Value <= todayEnd)
                .CountAsync();
        }

        /// <summary>
        /// Tính doanh thu theo giờ trong ngày
        /// = Sum(Transaction.Amount) với Status = "Paid" và CompletedAt.Date = today
        /// + Sum(ReservationDeposit.Amount) với DepositDate.Date = today VÀ Reservation.Status = "Completed"
        /// </summary>
        public async Task<Dictionary<int, decimal>> GetHourlyRevenueChartAsync()
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var todayStart = today.ToDateTime(TimeOnly.MinValue);
            var todayEnd = today.ToDateTime(TimeOnly.MaxValue);

            // Revenue from Transactions
            var transactions = await _context.Transactions
                .Where(t => t.Status == "Paid" && t.CompletedAt.HasValue)
                .Where(t => t.CompletedAt.Value >= todayStart && t.CompletedAt.Value <= todayEnd)
                .Select(t => new
                {
                    Hour = t.CompletedAt!.Value.Hour,
                    Amount = t.Amount
                })
                .ToListAsync();

            // Revenue from Deposits - CHỈ tính từ Reservation có Status = "Completed"
            var deposits = await _context.ReservationDeposits
                .Include(d => d.Reservation)
                .Where(d => d.DepositDate >= todayStart && 
                           d.DepositDate <= todayEnd &&
                           d.Reservation != null &&
                           d.Reservation.Status == "Completed")
                .Select(d => new
                {
                    Hour = d.DepositDate.Hour,
                    Amount = d.Amount
                })
                .ToListAsync();

            // Combine and group by hour
            var allRevenue = transactions
                .Select(t => new { t.Hour, t.Amount })
                .Concat(deposits.Select(d => new { d.Hour, d.Amount }))
                .Where(x => x.Hour >= 8 && x.Hour <= 22) // Chỉ lấy giờ từ 8h-22h
                .GroupBy(x => x.Hour)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));

            // Fill missing hours with 0 (chỉ từ 8h-22h)
            var result = new Dictionary<int, decimal>();
            for (int hour = 8; hour <= 22; hour++)
            {
                result[hour] = allRevenue.ContainsKey(hour) ? allRevenue[hour] : 0m;
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

            // Group by hour and count (chỉ lấy giờ từ 8h-22h)
            var hourlyOrders = orders
                .Where(o => o.Hour >= 8 && o.Hour <= 22)
                .GroupBy(o => o.Hour)
                .ToDictionary(g => g.Key, g => g.Count());

            // Fill missing hours with 0 (chỉ từ 8h-22h)
            var result = new Dictionary<int, int>();
            for (int hour = 8; hour <= 22; hour++)
            {
                result[hour] = hourlyOrders.ContainsKey(hour) ? hourlyOrders[hour] : 0;
            }

            return result;
        }
    }
}

