using WebSapaForestForStaff.DTOs.Owner;

namespace WebSapaForestForStaff.Models.Owner
{
    /// <summary>
    /// ViewModel cho Revenue View
    /// </summary>
    public class RevenueViewModel
    {
        public RevenueSummaryDto Summary { get; set; } = new();
        public List<RevenueDetailDto> Details { get; set; } = new();
        public List<RevenueTrendDataDto> TrendData { get; set; } = new();
        public PaymentMethodBreakdownDto PaymentBreakdown { get; set; } = new();
        public List<BranchComparisonDto> BranchComparison { get; set; } = new();

        // Filter properties
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? SelectedBranch { get; set; } = "ALL";
        public string? SelectedPaymentMethod { get; set; } = "ALL";

        // UI properties
        public string? ErrorMessage { get; set; }
        public bool HasData => Summary != null && Details != null;
    }
}

