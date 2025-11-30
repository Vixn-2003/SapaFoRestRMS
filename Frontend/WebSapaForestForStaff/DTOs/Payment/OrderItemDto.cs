namespace WebSapaForestForStaff.DTOs.Payment
{
    /// <summary>
    /// Chi tiết món ăn trong đơn hàng
    /// </summary>
    public class OrderItemDto
    {
        public int OrderDetailId { get; set; }
        public int? MenuItemId { get; set; }
        public string MenuItemName { get; set; } = string.Empty;
        public int? ComboId { get; set; }
        public string? ComboName { get; set; }
        public int Quantity { get; set; }
        public int QuantityUsed { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }

        // Legacy aliases
        public string Name
        {
            get => !string.IsNullOrEmpty(ComboName) ? ComboName : MenuItemName;
            set => MenuItemName = value;
        }

        public decimal Price
        {
            get => UnitPrice;
            set => UnitPrice = value;
        }
    }
}

