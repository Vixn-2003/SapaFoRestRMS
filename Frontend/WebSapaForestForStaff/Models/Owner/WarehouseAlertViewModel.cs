using WebSapaForestForStaff.DTOs.Owner;

namespace WebSapaForestForStaff.Models.Owner
{
    /// <summary>
    /// ViewModel cho Warehouse Alert View
    /// </summary>
    public class WarehouseAlertViewModel
    {
        public AlertSummaryCardsDto Summary { get; set; } = new();
        public List<LowStockItemDto> LowStockItems { get; set; } = new();
        public List<NearExpiryItemDto> NearExpiryItems { get; set; } = new();
        public List<ExpiredItemDto> ExpiredItems { get; set; } = new();
        public AlertCategoryDistributionDto CategoryDistribution { get; set; } = new();
        public List<StockLevelChartDto> StockLevelChart { get; set; } = new();
        public List<ExpiryTimelineDto> ExpiryTimeline { get; set; } = new();

        // UI properties
        public string? ErrorMessage { get; set; }
        public bool HasData => Summary != null;
    }
}

