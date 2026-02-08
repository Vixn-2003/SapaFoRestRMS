using System.Collections.Generic;
using System.Threading.Tasks;
using WebSapaForestForStaff.DTOs.AdminDashboard;

namespace WebSapaForestForStaff.Services.Api.Interfaces
{
    /// <summary>
    /// Interface for Admin Dashboard API Service
    /// Handles API calls for Admin Dashboard
    /// </summary>
    public interface IAdminDashboardApiService : IBaseApiService
    {
        /// <summary>
        /// Lấy toàn bộ dữ liệu dashboard
        /// </summary>
        Task<AdminDashboardDto?> GetDashboardAsync();

        /// <summary>
        /// Lấy dữ liệu revenue 7 ngày
        /// </summary>
        Task<List<RevenuePointDto>?> GetRevenueLast7DaysAsync();

        /// <summary>
        /// Lấy dữ liệu orders 7 ngày
        /// </summary>
        Task<List<OrderPointDto>?> GetOrdersLast7DaysAsync();

        /// <summary>
        /// Lấy tổng hợp alerts
        /// </summary>
        Task<AlertSummaryDto?> GetAlertSummaryAsync();

        /// <summary>
        /// Lấy recent logs
        /// </summary>
        Task<List<SystemLogDto>?> GetRecentLogsAsync();
    }
}

