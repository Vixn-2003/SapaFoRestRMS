using System;
using System.Collections.Generic;

namespace WebSapaForestForStaff.DTOs.AdminDashboard
{
    /// <summary>
    /// DTO chính cho Admin Dashboard (Frontend)
    /// </summary>
    public class AdminDashboardDto
    {
        public KpiCardsDto KpiCards { get; set; } = new();
        public UserRoleDistributionDto UserRoleDistribution { get; set; } = new();
        public List<RevenuePointDto> RevenueLast7Days { get; set; } = new();
        public List<OrderPointDto> OrdersLast7Days { get; set; } = new();
        public AlertSummaryDto WarehouseAlerts { get; set; } = new();
        public List<TopUserDto> Top5ActiveUsers { get; set; } = new();
        public List<TopCategoryDto> Top5BestSellingCategories { get; set; } = new();
        public List<SystemLogDto> RecentLogs { get; set; } = new();
    }

    public class KpiCardsDto
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int InactiveUsers { get; set; }
        public int TodayReservations { get; set; }
        public int TodayOrders { get; set; }
        public int CompletedPaymentsToday { get; set; }
        public int PendingPayments { get; set; }
        public decimal TodayRevenue { get; set; }
        public decimal MonthRevenue { get; set; }
        public int TotalAlertsCount { get; set; }
    }

    public class UserRoleDistributionDto
    {
        public int AdminCount { get; set; }
        public int ManagerCount { get; set; }
        public int StaffCount { get; set; }
        public int CashierCount { get; set; }
        public int WaiterCount { get; set; }
        public int KitchenCount { get; set; }
        public int OwnerCount { get; set; }
        public int CustomerCount { get; set; }
        public Dictionary<string, int> RoleDistribution { get; set; } = new();
    }

    public class RevenuePointDto
    {
        public DateTime Date { get; set; }
        public string DateLabel { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
    }

    public class OrderPointDto
    {
        public DateTime Date { get; set; }
        public string DateLabel { get; set; } = string.Empty;
        public int OrderCount { get; set; }
    }

    public class AlertSummaryDto
    {
        public int LowStockCount { get; set; }
        public int ExpiredIngredientsCount { get; set; }
        public int NearExpiryCount { get; set; }
        public int TotalAlerts => LowStockCount + ExpiredIngredientsCount + NearExpiryCount;
    }

    public class TopUserDto
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public int LoginCount { get; set; }
    }

    public class TopCategoryDto
    {
        public string CategoryName { get; set; } = string.Empty;
        public int ItemsSold { get; set; }
        public decimal Revenue { get; set; }
    }

    public class SystemLogDto
    {
        public DateTime Time { get; set; }
        public string TimeFormatted { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
    }
}

