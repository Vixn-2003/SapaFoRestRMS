using WebSapaForestForStaff.DTOs.Owner;

namespace WebSapaForestForStaff.Models.Owner
{
    /// <summary>
    /// ViewModel cho Owner Dashboard View
    /// </summary>
    public class OwnerDashboardViewModel
    {
        public KpiCardsDto KpiCards { get; set; } = new();
        public List<RevenueTrendDataDto> RevenueTrend { get; set; } = new();
        public List<TopSellingItemDto> TopSellingItems { get; set; } = new();
        public List<BranchComparisonDto> BranchComparison { get; set; } = new();
        public AlertsSummaryDto AlertsSummary { get; set; } = new();

        // Additional UI properties
        public string? ErrorMessage { get; set; }
        public bool HasData => KpiCards != null;
    }
}

