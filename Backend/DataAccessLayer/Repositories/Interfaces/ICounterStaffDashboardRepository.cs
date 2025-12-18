using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories.Interfaces
{
    /// <summary>
    /// Repository interface cho Counter Staff Dashboard - UC122
    /// </summary>
    public interface ICounterStaffDashboardRepository
    {
        /// <summary>
        /// Lấy số lượng reservation hôm nay
        /// </summary>
        Task<int> GetTodayReservationCountAsync();

        /// <summary>
        /// Lấy tổng revenue hôm nay (tổng tiền thanh toán)
        /// </summary>
        Task<decimal> GetTodayRevenueAsync();

        /// <summary>
        /// Lấy số lượng order đang mở (Confirmed, Pending)
        /// </summary>
        Task<int> GetActiveOrdersCountAsync();

        /// <summary>
        /// Lấy số order chờ thanh toán (Confirmed nhưng chưa Paid)
        /// </summary>
        Task<int> GetPendingPaymentOrdersAsync();

        /// <summary>
        /// Lấy số bàn active (đang có khách)
        /// </summary>
        Task<int> GetActiveTablesCountAsync();

        /// <summary>
        /// Lấy số giao dịch đã hoàn thành trong ca hiện tại
        /// </summary>
        Task<int> GetTransactionCountAsync();

        /// <summary>
        /// Lấy dữ liệu revenue chart theo giờ (hôm nay)
        /// </summary>
        Task<Dictionary<int, decimal>> GetHourlyRevenueChartAsync();

        /// <summary>
        /// Lấy dữ liệu order count chart theo giờ (hôm nay)
        /// </summary>
        Task<Dictionary<int, int>> GetHourlyOrdersChartAsync();
    }
}

