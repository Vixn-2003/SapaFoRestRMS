using System;
using System.Collections.Generic;

namespace WebSapaForestForStaff.DTOs.Owner
{
    /// <summary>
    /// Frontend DTO cho Owner Dashboard
    /// </summary>
    public class OwnerDashboardDto
    {
        public KpiCardsDto KpiCards { get; set; } = new();
        public List<RevenueTrendDataDto> RevenueTrend { get; set; } = new();
        public List<TopSellingItemDto> TopSellingItems { get; set; } = new();
        public List<BranchComparisonDto> BranchComparison { get; set; } = new();
        public AlertsSummaryDto AlertsSummary { get; set; } = new();
    }

    public class KpiCardsDto
    {
        public decimal TodayRevenue { get; set; }
        public decimal MonthlyRevenue { get; set; }
        public int TotalOrders { get; set; }
        public int ActiveCustomers { get; set; }
        public int LowStockAlertsCount { get; set; }
        public int NearExpiryAlertsCount { get; set; }
        public decimal TodayRevenueChangePercent { get; set; }
        public decimal MonthlyRevenueChangePercent { get; set; }
    }

    public class RevenueTrendDataDto
    {
        public string Date { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public int OrderCount { get; set; }
    }

    public class TopSellingItemDto
    {
        public string ItemName { get; set; } = string.Empty;
        public int QuantitySold { get; set; }
        public decimal Revenue { get; set; }
    }

    public class BranchComparisonDto
    {
        public string BranchName { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public int OrderCount { get; set; }
    }

    public class AlertsSummaryDto
    {
        public int LowStockCount { get; set; }
        public int NearExpiryCount { get; set; }
        public int ExpiredCount { get; set; }
    }
}

