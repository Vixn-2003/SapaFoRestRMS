namespace BusinessAccessLayer.DTOs.Kitchen
{
    /// <summary>
    /// DTO cho nguyên liệu cần lấy từ lô hàng
    /// </summary>
    public class IngredientPickupDTO
    {
        public int OrderDetailId { get; set; }
        public string MenuItemName { get; set; } = string.Empty;
        public int OrderQuantity { get; set; }
        public int OrderId { get; set; }
        public string? TableName { get; set; }
        
        public int IngredientId { get; set; }
        public string IngredientName { get; set; } = string.Empty;
        public string? UnitName { get; set; }
        
        public int BatchId { get; set; }
        public int WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public DateOnly? ExpiryDate { get; set; }
        public decimal QuantityToPick { get; set; } // Số lượng cần lấy từ lô này
        public decimal QuantityReserved { get; set; } // Tổng số lượng đã reserve trong lô này
        public bool IsUrgent { get; set; } // Món có ưu tiên không
    }
}

