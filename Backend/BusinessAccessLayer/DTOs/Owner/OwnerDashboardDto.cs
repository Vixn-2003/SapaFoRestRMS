using System;
using System.Collections.Generic;

namespace BusinessAccessLayer.DTOs.Owner
{
    /// <summary>
    /// DTO cho Owner Dashboard - tổng quan kinh doanh
    /// </summary>
    public class OwnerDashboardDto
    {
        public KpiCardsDto KpiCards { get; set; } = new();
        public List<RevenueTrendDataDto> RevenueTrend { get; set; } = new();
        public List<TopSellingItemDto> TopSellingItems { get; set; } = new();
        public List<BranchComparisonDto> BranchComparison { get; set; } = new();
        public AlertsSummaryDto AlertsSummary { get; set; } = new();
    }

    /// <summary>
    /// KPI Cards - các chỉ số chính
    /// </summary>
    public class KpiCardsDto
    {
        public decimal TodayRevenue { get; set; }
        public decimal MonthlyRevenue { get; set; }
        public int TotalOrders { get; set; }
        public int ActiveCustomers { get; set; }
        public int LowStockAlertsCount { get; set; }
        public int NearExpiryAlertsCount { get; set; }
        
        // Thêm so sánh với kỳ trước
        public decimal TodayRevenueChangePercent { get; set; }
        public decimal MonthlyRevenueChangePercent { get; set; }
    }

    /// <summary>
    /// Dữ liệu cho Revenue Trend Chart (Line Chart)
    /// </summary>
    public class RevenueTrendDataDto
    {
        public string Date { get; set; } = string.Empty; // Format: "2025-01-15" hoặc "15/01"
        public decimal Revenue { get; set; }
        public int OrderCount { get; set; }
    }

    /// <summary>
    /// Top Selling Items (Horizontal Bar Chart)
    /// </summary>
    public class TopSellingItemDto
    {
        public string ItemName { get; set; } = string.Empty;
        public int QuantitySold { get; set; }
        public decimal Revenue { get; set; }
    }

    /// <summary>
    /// Branch Comparison (Bar Chart)
    /// </summary>
    public class BranchComparisonDto
    {
        public string BranchName { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public int OrderCount { get; set; }
    }

    /// <summary>
    /// Alerts Summary (Pie/Donut Chart)
    /// </summary>
    public class AlertsSummaryDto
    {
        public int LowStockCount { get; set; }
        public int NearExpiryCount { get; set; }
        public int ExpiredCount { get; set; }
    }
}

