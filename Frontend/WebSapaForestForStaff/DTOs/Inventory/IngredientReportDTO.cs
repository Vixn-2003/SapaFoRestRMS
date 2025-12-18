namespace WebSapaForestForStaff.DTOs.Inventory
{
    public class IngredientReportDTO
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string CreatedBy { get; set; } = string.Empty;

        // Tổng quan
        public int TotalIngredients { get; set; }
        public int TotalBatches { get; set; }
        public int TotalWarehouses { get; set; }

        // Cảnh báo
        public int OutOfStockCount { get; set; }
        public int LowStockCount { get; set; }
        public int BelowReorderCount { get; set; }
        public int UrgentRestockCount { get; set; }
        public int ExpiredBatchCount { get; set; }
        public int ExpiringSoonBatchCount { get; set; }

        // Danh sách chi tiết
        public List<IngredientAlertDTO> OutOfStockItems { get; set; } = new();
        public List<IngredientAlertDTO> LowStockItems { get; set; } = new();
        public List<IngredientAlertDTO> UrgentRestockItems { get; set; } = new();
        public List<BatchAlertDTO> ExpiredBatches { get; set; } = new();
        public List<BatchAlertDTO> ExpiringSoonBatches { get; set; } = new();
        public List<IngredientDetailDTO> AllIngredients { get; set; } = new();
        public List<WarehouseStatDTOs> WarehouseStats { get; set; } = new();
    }

    public class IngredientAlertDTO
    {
        public string IngredientCode { get; set; } = string.Empty;
        public string IngredientName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal CurrentQuantity { get; set; }
        public decimal? ReorderLevel { get; set; }
        public decimal? QuantityExcludingExpired { get; set; }
    }

    public class BatchAlertDTO
    {
        public string BatchCode { get; set; } = string.Empty;
        public string IngredientName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal QuantityRemaining { get; set; }
        public DateTime? ImportDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public int DaysLeft { get; set; }
    }

    public class IngredientDetailDTO
    {
        public string IngredientCode { get; set; } = string.Empty;
        public string IngredientName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal TotalQuantity { get; set; }
        public decimal? ReorderLevel { get; set; }
        public string Status { get; set; } = string.Empty;
        public string StatusText { get; set; } = string.Empty;
    }

    public class WarehouseStatDTOs
    {
        public string WarehouseName { get; set; } = string.Empty;
        public int BatchCount { get; set; }
        public int IngredientCount { get; set; }
        public int ExpiredBatchCount { get; set; }
        public int ExpiringSoonBatchCount { get; set; }
    }
}
