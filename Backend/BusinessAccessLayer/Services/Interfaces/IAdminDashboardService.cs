using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessAccessLayer.DTOs.AdminDashboard;

namespace BusinessAccessLayer.Services.Interfaces
{
    /// <summary>
    /// Service interface cho Admin Dashboard
    /// </summary>
    public interface IAdminDashboardService
    {
        /// <summary>
        /// Lấy toàn bộ dữ liệu dashboard
        /// </summary>
        Task<AdminDashboardDto> GetDashboardDataAsync(CancellationToken ct = default);

        /// <summary>
        /// Lấy dữ liệu revenue 7 ngày (riêng lẻ nếu cần)
        /// </summary>
        Task<List<RevenuePointDto>> GetRevenueLast7DaysAsync(CancellationToken ct = default);

        /// <summary>
        /// Lấy dữ liệu orders 7 ngày (riêng lẻ nếu cần)
        /// </summary>
        Task<List<OrderPointDto>> GetOrdersLast7DaysAsync(CancellationToken ct = default);

        /// <summary>
        /// Lấy alert summary (riêng lẻ nếu cần)
        /// </summary>
        Task<AlertSummaryDto> GetAlertSummaryAsync(CancellationToken ct = default);

        /// <summary>
        /// Lấy recent logs (riêng lẻ nếu cần)
        /// </summary>
        Task<List<SystemLogDto>> GetRecentLogsAsync(CancellationToken ct = default);
    }
}

