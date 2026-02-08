namespace WebSapaForestForStaff.DTOs.Inventory
{
    public class ExportManagementViewModel
    {
        public List<StockTransactionInventoryDTO> ExportData { get; set; } = new();
        public string? ErrorMessage { get; set; }

        // THÊM PROPERTY MỚI NÀY
        public int TodayTransactions { get; set; }

        // Computed properties cho thống kê
        public int TotalTransactions => ExportData?.Count ?? 0;
        public List<WarehouseInfo> Warehouses => ExportData?
            .Where(x => x.WarehouseId > 0 && !string.IsNullOrEmpty(x.WarehouseName))
            .Select(x => new WarehouseInfo { Id = x.WarehouseId, Name = x.WarehouseName })
            .DistinctBy(x => x.Id)
            .OrderBy(x => x.Name)
            .ToList() ?? new List<WarehouseInfo>();
        public List<IngredientInfo> Ingredients => ExportData?
            .Where(x => x.IngredientId > 0 && !string.IsNullOrEmpty(x.IngredientName))
            .Select(x => new IngredientInfo { Id = x.IngredientId, Name = x.IngredientName })
            .DistinctBy(x => x.Id)
            .OrderBy(x => x.Name)
            .ToList() ?? new List<IngredientInfo>();
    }

    public class WarehouseInfo
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class IngredientInfo
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
