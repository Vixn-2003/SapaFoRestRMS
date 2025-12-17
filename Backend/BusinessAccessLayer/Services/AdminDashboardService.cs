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
            // Lấy dữ liệu tuần tự để tránh DbContext concurrency issues
            // User Statistics
            var totalUsers = await _dashboardRepository.GetTotalUsersAsync();
            var activeUsers = await _dashboardRepository.GetActiveUsersAsync();
            var inactiveUsers = await _dashboardRepository.GetInactiveUsersAsync();
            var usersByRole = await _dashboardRepository.GetUsersByRoleAsync();

            // Reservations & Orders
            var todayReservations = await _dashboardRepository.GetTodayReservationsAsync();
            var todayOrders = await _dashboardRepository.GetTodayOrdersAsync();
            var completedPayments = await _dashboardRepository.GetCompletedPaymentsTodayAsync();
            var pendingPayments = await _dashboardRepository.GetPendingPaymentsAsync();

            // Revenue Data
            var todayRevenue = await _dashboardRepository.GetTodayRevenueAsync();
            var monthRevenue = await _dashboardRepository.GetMonthRevenueAsync();
            var revenueLast7Days = await _dashboardRepository.GetRevenueLast7DaysAsync();
            var ordersLast7Days = await _dashboardRepository.GetOrdersLast7DaysAsync();

            // Warehouse Alerts
            var lowStock = await _dashboardRepository.GetLowStockCountAsync();
            var expiredIngredients = await _dashboardRepository.GetExpiredIngredientsCountAsync();
            var nearExpiry = await _dashboardRepository.GetNearExpiryIngredientsCountAsync();

            // Top Lists
            var top5ActiveUsers = await _dashboardRepository.GetTop5ActiveUsersAsync();
            var top5Categories = await _dashboardRepository.GetTop5BestSellingCategoriesAsync();
            var recentLogs = await _dashboardRepository.GetRecentSystemLogsAsync();

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
    }
}

