using System;
using System.Collections.Generic;

namespace BusinessAccessLayer.DTOs.AdminDashboard
{
    /// <summary>
    /// DTO chính cho Admin Dashboard
    /// Chứa toàn bộ dữ liệu cần hiển thị trên dashboard
    /// </summary>
    public class AdminDashboardDto
    {
        /// <summary>
        /// KPI Cards - Các chỉ số chính
        /// </summary>
        public KpiCardsDto KpiCards { get; set; } = new();

        /// <summary>
        /// Phân bố người dùng theo vai trò (Role)
        /// </summary>
        public UserRoleDistributionDto UserRoleDistribution { get; set; } = new();

        /// <summary>
        /// Dữ liệu revenue 7 ngày gần nhất (Line Chart)
        /// </summary>
        public List<RevenuePointDto> RevenueLast7Days { get; set; } = new();

        /// <summary>
        /// Dữ liệu orders 7 ngày gần nhất (Bar Chart)
        /// </summary>
        public List<OrderPointDto> OrdersLast7Days { get; set; } = new();

        /// <summary>
        /// Tổng hợp cảnh báo kho (Warehouse Alerts)
        /// </summary>
        public AlertSummaryDto WarehouseAlerts { get; set; } = new();

        /// <summary>
        /// Top 5 users hoạt động nhiều nhất
        /// </summary>
        public List<TopUserDto> Top5ActiveUsers { get; set; } = new();

        /// <summary>
        /// Top 5 categories bán chạy nhất
        /// </summary>
        public List<TopCategoryDto> Top5BestSellingCategories { get; set; } = new();

        /// <summary>
        /// 10 system logs gần nhất
        /// </summary>
        public List<SystemLogDto> RecentLogs { get; set; } = new();
    }

    /// <summary>
    /// KPI Cards - Các chỉ số chính ở đầu dashboard
    /// </summary>
    public class KpiCardsDto
    {
        // User Statistics
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int InactiveUsers { get; set; }

        // Activity Today
        public int TodayReservations { get; set; }
        public int TodayOrders { get; set; }
        public int CompletedPaymentsToday { get; set; }
        public int PendingPayments { get; set; }

        // Revenue
        public decimal TodayRevenue { get; set; }
        public decimal MonthRevenue { get; set; }

        // Alerts
        public int TotalAlertsCount { get; set; } // Tổng cảnh báo (low stock + expired + near expiry)
    }

    /// <summary>
    /// Phân bố người dùng theo vai trò
    /// </summary>
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

        /// <summary>
        /// Dictionary chứa tất cả roles (flexible)
        /// Key: Role Name, Value: Count
        /// </summary>
        public Dictionary<string, int> RoleDistribution { get; set; } = new();
    }

    /// <summary>
    /// Điểm dữ liệu revenue theo ngày
    /// </summary>
    public class RevenuePointDto
    {
        public DateTime Date { get; set; }
        public string DateLabel { get; set; } = string.Empty; // Format: "dd/MM"
        public decimal Revenue { get; set; }
    }

    /// <summary>
    /// Điểm dữ liệu orders theo ngày
    /// </summary>
    public class OrderPointDto
    {
        public DateTime Date { get; set; }
        public string DateLabel { get; set; } = string.Empty; // Format: "dd/MM"
        public int OrderCount { get; set; }
    }

    /// <summary>
    /// Tổng hợp cảnh báo kho hàng
    /// </summary>
    public class AlertSummaryDto
    {
        public int LowStockCount { get; set; }
        public int ExpiredIngredientsCount { get; set; }
        public int NearExpiryCount { get; set; }

        public int TotalAlerts => LowStockCount + ExpiredIngredientsCount + NearExpiryCount;
    }

    /// <summary>
    /// Top user hoạt động nhiều nhất
    /// </summary>
    public class TopUserDto
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public int LoginCount { get; set; }
    }

    /// <summary>
    /// Top category bán chạy nhất
    /// </summary>
    public class TopCategoryDto
    {
        public string CategoryName { get; set; } = string.Empty;
        public int ItemsSold { get; set; }
        public decimal Revenue { get; set; }
    }

    /// <summary>
    /// System log entry
    /// </summary>
    public class SystemLogDto
    {
        public DateTime Time { get; set; }
        public string TimeFormatted { get; set; } = string.Empty; // Format: "dd/MM/yyyy HH:mm"
        public string Username { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
    }
}

