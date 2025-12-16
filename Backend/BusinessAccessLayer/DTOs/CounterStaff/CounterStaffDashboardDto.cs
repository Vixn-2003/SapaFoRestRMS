using System;
using System.Collections.Generic;

namespace BusinessAccessLayer.DTOs.CounterStaff
{
    /// <summary>
    /// DTO cho Counter Staff Dashboard - UC122
    /// Hiển thị overview realtime mỗi ngày
    /// </summary>
    public class CounterStaffDashboardDto
    {
        /// <summary>
        /// Số lượng reservation hôm nay
        /// </summary>
        public int TodayReservations { get; set; }

        /// <summary>
        /// Revenue hôm nay (tổng tiền thanh toán)
        /// </summary>
        public decimal TodayRevenue { get; set; }

        /// <summary>
        /// Tổng số order đang mở
        /// </summary>
        public int ActiveOrders { get; set; }

        /// <summary>
        /// Số order chờ thanh toán
        /// </summary>
        public int PendingPayments { get; set; }

        /// <summary>
        /// Số bàn active trong nhà hàng
        /// </summary>
        public int ActiveTables { get; set; }

        /// <summary>
        /// Số giao dịch đã xử lý trong ca
        /// </summary>
        public int CompletedTransactions { get; set; }

        /// <summary>
        /// Revenue trend (today timeline – hour-based)
        /// </summary>
        public List<HourlyRevenuePoint> RevenueChart { get; set; } = new();

        /// <summary>
        /// Order count per hour
        /// </summary>
        public List<HourlyOrderPoint> OrderChart { get; set; } = new();
    }

    /// <summary>
    /// Điểm dữ liệu revenue theo giờ
    /// </summary>
    public class HourlyRevenuePoint
    {
        public int Hour { get; set; }
        public decimal Revenue { get; set; }
    }

    /// <summary>
    /// Điểm dữ liệu số đơn hàng theo giờ
    /// </summary>
    public class HourlyOrderPoint
    {
        public int Hour { get; set; }
        public int OrderCount { get; set; }
    }
}

