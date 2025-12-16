using System;
using System.Collections.Generic;

namespace WebSapaForestForStaff.DTOs.CounterStaff
{
    public class CounterStaffDashboardDto
    {
        public int TodayReservations { get; set; }
        public decimal TodayRevenue { get; set; }
        public int ActiveOrders { get; set; }
        public int PendingPayments { get; set; }
        public int ActiveTables { get; set; }
        public int CompletedTransactions { get; set; }
        public List<HourlyRevenuePoint> RevenueChart { get; set; } = new();
        public List<HourlyOrderPoint> OrderChart { get; set; } = new();
    }

    public class HourlyRevenuePoint
    {
        public int Hour { get; set; }
        public decimal Revenue { get; set; }
    }

    public class HourlyOrderPoint
    {
        public int Hour { get; set; }
        public int OrderCount { get; set; }
    }
}

