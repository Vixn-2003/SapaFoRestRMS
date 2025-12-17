namespace WebSapaForestForStaff.DTOs.Inventory
{
    public class ExportReportDTO
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public int TotalTransactions { get; set; }
        public List<StockTransactionInventoryDTO> Transactions { get; set; } = new();

        // Thống kê
        public List<TopIngredientDTO> TopIngredients { get; set; } = new();
        public List<AbnormalExportDTO> AbnormalExports { get; set; } = new();
        public List<WarehouseStatDTO> WarehouseStats { get; set; } = new();
        public List<ComparisonDTO> Comparisons { get; set; } = new();
    }

    public class TopIngredientDTO
    {
        public string IngredientName { get; set; } = string.Empty;
        public decimal TotalQuantity { get; set; }
        public string UnitName { get; set; } = string.Empty;
    }

    public class AbnormalExportDTO
    {
        public string IngredientName { get; set; } = string.Empty;
        public decimal TodayQuantity { get; set; }
        public decimal AvgQuantity { get; set; }
        public decimal PercentChange { get; set; }
        public string UnitName { get; set; } = string.Empty;
    }

    public class WarehouseStatDTO
    {
        public string WarehouseName { get; set; } = string.Empty;
        public int TransactionCount { get; set; }
    }

    public class ComparisonDTO
    {
        public string IngredientName { get; set; } = string.Empty;
        public decimal TodayQuantity { get; set; }
        public decimal AvgQuantity { get; set; }
        public decimal PercentChange { get; set; }
        public string UnitName { get; set; } = string.Empty;
    }
}
