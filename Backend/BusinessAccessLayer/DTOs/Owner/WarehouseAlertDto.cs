using System;
using System.Collections.Generic;

namespace BusinessAccessLayer.DTOs.Owner
{
    /// <summary>
    /// Response DTO cho Warehouse Alert View
    /// </summary>
    public class WarehouseAlertResponseDto
    {
        public AlertSummaryCardsDto Summary { get; set; } = new();
        public List<LowStockItemDto> LowStockItems { get; set; } = new();
        public List<NearExpiryItemDto> NearExpiryItems { get; set; } = new();
        public List<ExpiredItemDto> ExpiredItems { get; set; } = new();
        public AlertCategoryDistributionDto CategoryDistribution { get; set; } = new();
        public List<StockLevelChartDto> StockLevelChart { get; set; } = new();
        public List<ExpiryTimelineDto> ExpiryTimeline { get; set; } = new();
    }

    /// <summary>
    /// Alert Summary Cards
    /// </summary>
    public class AlertSummaryCardsDto
    {
        public int LowStockCount { get; set; }
        public int NearExpiryCount { get; set; }
        public int ExpiredCount { get; set; }
        public int TotalAlertsCount { get; set; }
    }

    /// <summary>
    /// Low Stock Items (Table + Chart)
    /// </summary>
    public class LowStockItemDto
    {
        public int IngredientId { get; set; }
        public string IngredientName { get; set; } = string.Empty;
        public string IngredientCode { get; set; } = string.Empty;
        public decimal CurrentQuantity { get; set; }
        public decimal ReorderLevel { get; set; }
        public string Unit { get; set; } = string.Empty;
        public string WarehouseName { get; set; } = string.Empty;
        public decimal ShortageAmount { get; set; } // ReorderLevel - CurrentQuantity
        public string Severity { get; set; } = "Low"; // "Critical", "Low", "Warning"
    }

    /// <summary>
    /// Near Expiry Items (Table + Chart)
    /// </summary>
    public class NearExpiryItemDto
    {
        public int BatchId { get; set; }
        public string IngredientName { get; set; } = string.Empty;
        public string IngredientCode { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string Unit { get; set; } = string.Empty;
        public DateTime ExpiryDate { get; set; }
        public int DaysUntilExpiry { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public string Severity { get; set; } = "Warning"; // "Critical" (<3 days), "Warning" (3-7 days)
    }

    /// <summary>
    /// Expired Items (Table)
    /// </summary>
    public class ExpiredItemDto
    {
        public int BatchId { get; set; }
        public string IngredientName { get; set; } = string.Empty;
        public string IngredientCode { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string Unit { get; set; } = string.Empty;
        public DateTime ExpiryDate { get; set; }
        public int DaysExpired { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
    }

    /// <summary>
    /// Alert Category Distribution (Donut Chart)
    /// </summary>
    public class AlertCategoryDistributionDto
    {
        public int LowStockCount { get; set; }
        public int NearExpiryCount { get; set; }
        public int ExpiredCount { get; set; }
    }

    /// <summary>
    /// Stock Level Chart Data (Vertical Bar Chart)
    /// Hiển thị Quantity vs ReorderLevel
    /// </summary>
    public class StockLevelChartDto
    {
        public string IngredientName { get; set; } = string.Empty;
        public decimal CurrentQuantity { get; set; }
        public decimal ReorderLevel { get; set; }
    }

    /// <summary>
    /// Expiry Timeline Chart Data (Scatter/Bar Chart)
    /// Hiển thị các batch theo thời gian hết hạn
    /// </summary>
    public class ExpiryTimelineDto
    {
        public string IngredientName { get; set; } = string.Empty;
        public int DaysUntilExpiry { get; set; }
        public decimal Quantity { get; set; }
        public DateTime ExpiryDate { get; set; }
    }
}

