namespace BusinessAccessLayer.DTOs.Kitchen
{
    /// <summary>
    /// DTO cho thông báo thiếu nguyên liệu
    /// </summary>
    public class IngredientShortageDTO
    {
        public int OrderDetailId { get; set; }
        public string MenuItemName { get; set; } = string.Empty;
        public int OrderId { get; set; }
        public string? TableName { get; set; }
        public int IngredientId { get; set; }
        public string IngredientName { get; set; } = string.Empty;
        public string? UnitName { get; set; }
        public decimal RequiredQuantity { get; set; } // Số lượng cần
        public decimal ReservedQuantity { get; set; } // Số lượng đã reserve
        public decimal ShortageQuantity { get; set; } // Số lượng còn thiếu
        public bool IsUrgent { get; set; } // Món có ưu tiên không
    }
}

