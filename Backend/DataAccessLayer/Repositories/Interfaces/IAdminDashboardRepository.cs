using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories.Interfaces
{
    /// <summary>
    /// Repository interface cho Admin Dashboard
    /// Cung cấp dữ liệu tổng quan hệ thống cho Admin
    /// </summary>
    public interface IAdminDashboardRepository
    {
        // ========== USER STATISTICS ==========
        /// <summary>
        /// Lấy tổng số người dùng trong hệ thống
        /// </summary>
        Task<int> GetTotalUsersAsync();

        /// <summary>
        /// Lấy số lượng người dùng đang active
        /// </summary>
        Task<int> GetActiveUsersAsync();

        /// <summary>
        /// Lấy số lượng người dùng inactive
        /// </summary>
        Task<int> GetInactiveUsersAsync();

        /// <summary>
        /// Lấy phân bố người dùng theo vai trò (Role)
        /// Returns: Dictionary<RoleName, Count>
        /// </summary>
        Task<Dictionary<string, int>> GetUsersByRoleAsync();

        // ========== SYSTEM ACTIVITY ==========
        /// <summary>
        /// Lấy tổng số reservation hôm nay
        /// </summary>
        Task<int> GetTodayReservationsAsync();

        /// <summary>
        /// Lấy tổng số orders hôm nay
        /// </summary>
        Task<int> GetTodayOrdersAsync();

        /// <summary>
        /// Lấy số lượng payment đã hoàn thành hôm nay
        /// </summary>
        Task<int> GetCompletedPaymentsTodayAsync();

        /// <summary>
        /// Lấy số lượng payment đang pending
        /// </summary>
        Task<int> GetPendingPaymentsAsync();

        // ========== REVENUE STATISTICS ==========
        /// <summary>
        /// Lấy tổng revenue hôm nay
        /// </summary>
        Task<decimal> GetTodayRevenueAsync();

        /// <summary>
        /// Lấy revenue 7 ngày gần nhất (mỗi điểm là 1 ngày)
        /// Returns: List<(Date, Revenue)>
        /// </summary>
        Task<List<(DateTime Date, decimal Revenue)>> GetRevenueLast7DaysAsync();

        /// <summary>
        /// Lấy revenue tháng này
        /// </summary>
        Task<decimal> GetMonthRevenueAsync();

        // ========== ORDER STATISTICS ==========
        /// <summary>
        /// Lấy số lượng orders 7 ngày gần nhất (mỗi điểm là 1 ngày)
        /// Returns: List<(Date, OrderCount)>
        /// </summary>
        Task<List<(DateTime Date, int OrderCount)>> GetOrdersLast7DaysAsync();

        // ========== WAREHOUSE ALERTS ==========
        /// <summary>
        /// Lấy số lượng nguyên liệu sắp hết (Low Stock)
        /// </summary>
        Task<int> GetLowStockCountAsync();

        /// <summary>
        /// Lấy số lượng nguyên liệu đã hết hạn
        /// </summary>
        Task<int> GetExpiredIngredientsCountAsync();

        /// <summary>
        /// Lấy số lượng nguyên liệu sắp hết hạn (trong 7 ngày tới)
        /// </summary>
        Task<int> GetNearExpiryIngredientsCountAsync();

        // ========== TOP ANALYTICS ==========
        /// <summary>
        /// Lấy Top 5 users hoạt động nhiều nhất (theo số lần đăng nhập)
        /// Returns: List<(UserId, Username, FullName, LoginCount)>
        /// </summary>
        Task<List<(int UserId, string Username, string FullName, int LoginCount)>> GetTop5ActiveUsersAsync();

        /// <summary>
        /// Lấy Top 5 categories bán chạy nhất
        /// Returns: List<(CategoryName, ItemsSold, Revenue)>
        /// </summary>
        Task<List<(string CategoryName, int ItemsSold, decimal Revenue)>> GetTop5BestSellingCategoriesAsync();

        // ========== SYSTEM LOGS ==========
        /// <summary>
        /// Lấy 10 system logs gần nhất
        /// Returns: List<(Time, Username, Action)>
        /// </summary>
        Task<List<(DateTime Time, string Username, string Action)>> GetRecentSystemLogsAsync();
    }
}

