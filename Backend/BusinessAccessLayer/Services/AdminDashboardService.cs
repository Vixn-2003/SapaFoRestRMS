using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BusinessAccessLayer.DTOs.AdminDashboard;
using BusinessAccessLayer.Services.Interfaces;
using DataAccessLayer.Repositories.Interfaces;

namespace BusinessAccessLayer.Services
{
    /// <summary>
    /// Service implementation cho Admin Dashboard
    /// </summary>
    public class AdminDashboardService : IAdminDashboardService
    {
        private readonly IAdminDashboardRepository _dashboardRepository;

        public AdminDashboardService(IAdminDashboardRepository dashboardRepository)
        {
            _dashboardRepository = dashboardRepository;
        }

        public async Task<AdminDashboardDto> GetDashboardDataAsync(CancellationToken ct = default)
        {
            // Lấy dữ liệu tuần tự để tránh DbContext concurrency issues với error handling
            // User Statistics
            var totalUsers = await SafeExecuteAsync(() => _dashboardRepository.GetTotalUsersAsync(), 0);
            var activeUsers = await SafeExecuteAsync(() => _dashboardRepository.GetActiveUsersAsync(), 0);
            var inactiveUsers = await SafeExecuteAsync(() => _dashboardRepository.GetInactiveUsersAsync(), 0);
            var usersByRole = await SafeExecuteAsync(() => _dashboardRepository.GetUsersByRoleAsync(), new Dictionary<string, int>());

            // Reservations & Orders
            var todayReservations = await SafeExecuteAsync(() => _dashboardRepository.GetTodayReservationsAsync(), 0);
            var todayOrders = await SafeExecuteAsync(() => _dashboardRepository.GetTodayOrdersAsync(), 0);
            var completedPayments = await SafeExecuteAsync(() => _dashboardRepository.GetCompletedPaymentsTodayAsync(), 0);
            var pendingPayments = await SafeExecuteAsync(() => _dashboardRepository.GetPendingPaymentsAsync(), 0);

            // Revenue Data
            var todayRevenue = await SafeExecuteAsync(() => _dashboardRepository.GetTodayRevenueAsync(), 0m);
            var monthRevenue = await SafeExecuteAsync(() => _dashboardRepository.GetMonthRevenueAsync(), 0m);
            var revenueLast7Days = await SafeExecuteAsync(() => _dashboardRepository.GetRevenueLast7DaysAsync(), new List<(DateTime Date, decimal Revenue)>());
            var ordersLast7Days = await SafeExecuteAsync(() => _dashboardRepository.GetOrdersLast7DaysAsync(), new List<(DateTime Date, int OrderCount)>());

            // Warehouse Alerts
            var lowStock = await SafeExecuteAsync(() => _dashboardRepository.GetLowStockCountAsync(), 0);
            var expiredIngredients = await SafeExecuteAsync(() => _dashboardRepository.GetExpiredIngredientsCountAsync(), 0);
            var nearExpiry = await SafeExecuteAsync(() => _dashboardRepository.GetNearExpiryIngredientsCountAsync(), 0);

            // Top Lists
            var top5ActiveUsers = await SafeExecuteAsync(() => _dashboardRepository.GetTop5ActiveUsersAsync(), new List<(int UserId, string Username, string FullName, int LoginCount)>());
            var top5Categories = await SafeExecuteAsync(() => _dashboardRepository.GetTop5BestSellingCategoriesAsync(), new List<(string CategoryName, int ItemsSold, decimal Revenue)>());
            var recentLogs = await SafeExecuteAsync(() => _dashboardRepository.GetRecentSystemLogsAsync(), new List<(DateTime Time, string Username, string Action)>());

            // Build KPI Cards
            var kpiCards = new KpiCardsDto
            {
                TotalUsers = totalUsers,
                ActiveUsers = activeUsers,
                InactiveUsers = inactiveUsers,
                TodayReservations = todayReservations,
                TodayOrders = todayOrders,
                CompletedPaymentsToday = completedPayments,
                PendingPayments = pendingPayments,
                TodayRevenue = todayRevenue,
                MonthRevenue = monthRevenue,
                TotalAlertsCount = lowStock + expiredIngredients + nearExpiry
            };

            // Build User Role Distribution
            var roleDistribution = new UserRoleDistributionDto
            {
                RoleDistribution = usersByRole,
                AdminCount = usersByRole.ContainsKey("Admin") ? usersByRole["Admin"] : 0,
                ManagerCount = usersByRole.ContainsKey("Manager") ? usersByRole["Manager"] : 0,
                StaffCount = usersByRole.ContainsKey("Staff") ? usersByRole["Staff"] : 0,
                CashierCount = usersByRole.ContainsKey("Cashier") ? usersByRole["Cashier"] : 0,
                WaiterCount = usersByRole.ContainsKey("Waiter") ? usersByRole["Waiter"] : 0,
                KitchenCount = usersByRole.ContainsKey("Kitchen") ? usersByRole["Kitchen"] : 0,
                OwnerCount = usersByRole.ContainsKey("Owner") ? usersByRole["Owner"] : 0,
                CustomerCount = usersByRole.ContainsKey("Customer") ? usersByRole["Customer"] : 0
            };

            // Build Revenue Chart Data
            var revenuePoints = revenueLast7Days.Select(r => new RevenuePointDto
            {
                Date = r.Date,
                DateLabel = r.Date.ToString("dd/MM"),
                Revenue = r.Revenue
            }).ToList();

            // Build Orders Chart Data
            var orderPoints = ordersLast7Days.Select(o => new OrderPointDto
            {
                Date = o.Date,
                DateLabel = o.Date.ToString("dd/MM"),
                OrderCount = o.OrderCount
            }).ToList();

            // Build Warehouse Alerts
            var warehouseAlerts = new AlertSummaryDto
            {
                LowStockCount = lowStock,
                ExpiredIngredientsCount = expiredIngredients,
                NearExpiryCount = nearExpiry
            };

            // Build Top 5 Active Users
            var topUsers = top5ActiveUsers.Select(u => new TopUserDto
            {
                UserId = u.UserId,
                Username = u.Username,
                FullName = u.FullName,
                LoginCount = u.LoginCount
            }).ToList();

            // Build Top 5 Best Selling Categories
            var topCategories = top5Categories.Select(c => new TopCategoryDto
            {
                CategoryName = c.CategoryName,
                ItemsSold = c.ItemsSold,
                Revenue = c.Revenue
            }).ToList();

            // Build Recent Logs
            var systemLogs = recentLogs.Select(l => new SystemLogDto
            {
                Time = l.Time,
                TimeFormatted = l.Time.ToString("dd/MM/yyyy HH:mm"),
                Username = l.Username,
                Action = l.Action
            }).ToList();

            // Build final DTO
            var dashboard = new AdminDashboardDto
            {
                KpiCards = kpiCards,
                UserRoleDistribution = roleDistribution,
                RevenueLast7Days = revenuePoints,
                OrdersLast7Days = orderPoints,
                WarehouseAlerts = warehouseAlerts,
                Top5ActiveUsers = topUsers,
                Top5BestSellingCategories = topCategories,
                RecentLogs = systemLogs
            };

            return dashboard;
        }

        public async Task<List<RevenuePointDto>> GetRevenueLast7DaysAsync(CancellationToken ct = default)
        {
            var revenueLast7Days = await _dashboardRepository.GetRevenueLast7DaysAsync();
            return revenueLast7Days.Select(r => new RevenuePointDto
            {
                Date = r.Date,
                DateLabel = r.Date.ToString("dd/MM"),
                Revenue = r.Revenue
            }).ToList();
        }

        public async Task<List<OrderPointDto>> GetOrdersLast7DaysAsync(CancellationToken ct = default)
        {
            var ordersLast7Days = await _dashboardRepository.GetOrdersLast7DaysAsync();
            return ordersLast7Days.Select(o => new OrderPointDto
            {
                Date = o.Date,
                DateLabel = o.Date.ToString("dd/MM"),
                OrderCount = o.OrderCount
            }).ToList();
        }

        public async Task<AlertSummaryDto> GetAlertSummaryAsync(CancellationToken ct = default)
        {
            var lowStock = await _dashboardRepository.GetLowStockCountAsync();
            var expiredIngredients = await _dashboardRepository.GetExpiredIngredientsCountAsync();
            var nearExpiry = await _dashboardRepository.GetNearExpiryIngredientsCountAsync();

            return new AlertSummaryDto
            {
                LowStockCount = lowStock,
                ExpiredIngredientsCount = expiredIngredients,
                NearExpiryCount = nearExpiry
            };
        }

        public async Task<List<SystemLogDto>> GetRecentLogsAsync(CancellationToken ct = default)
        {
            var recentLogs = await _dashboardRepository.GetRecentSystemLogsAsync();
            return recentLogs.Select(l => new SystemLogDto
            {
                Time = l.Time,
                TimeFormatted = l.Time.ToString("dd/MM/yyyy HH:mm"),
                Username = l.Username,
                Action = l.Action
            }).ToList();
        }

        /// <summary>
        /// Helper method to safely execute repository methods with fallback values
        /// </summary>
        private async Task<T> SafeExecuteAsync<T>(Func<Task<T>> action, T fallbackValue)
        {
            try
            {
                return await action();
            }
            catch (Exception ex)
            {
                // Log the exception (you might want to use a proper logging framework)
                Console.WriteLine($"Error in AdminDashboardService: {ex.Message}");
                return fallbackValue;
            }
        }
    }
}

