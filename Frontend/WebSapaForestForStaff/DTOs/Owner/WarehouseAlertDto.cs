using System;
using System.Collections.Generic;

namespace WebSapaForestForStaff.DTOs.Owner
{
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

    public class AlertSummaryCardsDto
    {
        public int LowStockCount { get; set; }
        public int NearExpiryCount { get; set; }
        public int ExpiredCount { get; set; }
        public int TotalAlertsCount { get; set; }
    }

    public class LowStockItemDto
    {
        public int IngredientId { get; set; }
        public string IngredientName { get; set; } = string.Empty;
        public string IngredientCode { get; set; } = string.Empty;
        public decimal CurrentQuantity { get; set; }
        public decimal ReorderLevel { get; set; }
        public string Unit { get; set; } = string.Empty;
        public string WarehouseName { get; set; } = string.Empty;
        public decimal ShortageAmount { get; set; }
        public string Severity { get; set; } = "Low";
    }

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
        public string Severity { get; set; } = "Warning";
    }

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

    public class AlertCategoryDistributionDto
    {
        public int LowStockCount { get; set; }
        public int NearExpiryCount { get; set; }
        public int ExpiredCount { get; set; }
    }

    public class StockLevelChartDto
    {
        public string IngredientName { get; set; } = string.Empty;
        public decimal CurrentQuantity { get; set; }
        public decimal ReorderLevel { get; set; }
    }

    public class ExpiryTimelineDto
    {
        public string IngredientName { get; set; } = string.Empty;
        public int DaysUntilExpiry { get; set; }
        public decimal Quantity { get; set; }
        public DateTime ExpiryDate { get; set; }
    }
}

