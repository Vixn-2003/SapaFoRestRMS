namespace WebSapaForestForStaff.DTOs.Payment
{
    /// <summary>
    /// Chi tiết món ăn trong đơn hàng
    /// </summary>
    public class OrderItemDto
    {
        public int OrderDetailId { get; set; }
        public int MenuItemId { get; set; }
        public string MenuItemName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public int QuantityUsed { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }

        // Legacy aliases
        public string Name
        {
            get => MenuItemName;
            set => MenuItemName = value;
        }

        public decimal Price
        {
            get => UnitPrice;
            set => UnitPrice = value;
        }
    }
}

