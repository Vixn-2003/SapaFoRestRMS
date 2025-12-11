using System;
using System.Collections.Generic;

namespace BusinessAccessLayer.DTOs.Owner
{
    /// <summary>
    /// Request DTO cho Revenue Filter
    /// </summary>
    public class RevenueFilterRequestDto
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? BranchName { get; set; } // "ALL" hoặc tên chi nhánh cụ thể
        public string? PaymentMethod { get; set; } // "ALL", "Cash", "QR", "Combined"
    }

    /// <summary>
    /// Response DTO cho Revenue View
    /// </summary>
    public class RevenueResponseDto
    {
        public RevenueSummaryDto Summary { get; set; } = new();
        public List<RevenueDetailDto> Details { get; set; } = new();
        public List<RevenueTrendDataDto> TrendData { get; set; } = new();
        public PaymentMethodBreakdownDto PaymentBreakdown { get; set; } = new();
        public List<BranchComparisonDto> BranchComparison { get; set; } = new();
    }

    /// <summary>
    /// Revenue Summary Cards
    /// </summary>
    public class RevenueSummaryDto
    {
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public decimal AveragePerOrder { get; set; }
        public decimal CashRevenue { get; set; }
        public decimal QrRevenue { get; set; }
        public decimal CombinedRevenue { get; set; }
    }

    /// <summary>
    /// Chi tiết từng giao dịch (cho table)
    /// </summary>
    public class RevenueDetailDto
    {
        public int OrderId { get; set; }
        public string TransactionCode { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? CustomerName { get; set; }
        public string? BranchName { get; set; }
    }

    /// <summary>
    /// Payment Method Breakdown (Pie Chart)
    /// </summary>
    public class PaymentMethodBreakdownDto
    {
        public decimal CashAmount { get; set; }
        public decimal QrAmount { get; set; }
        public decimal CombinedAmount { get; set; }
        
        public int CashCount { get; set; }
        public int QrCount { get; set; }
        public int CombinedCount { get; set; }
    }

    // Note: RevenueTrendDataDto and BranchComparisonDto are defined in OwnerDashboardDto.cs
    // to avoid duplication since they are shared between Dashboard and Revenue views
}
