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
        
        // === NEW: Support for BillingType and Kitchen Status ===
        
        /// <summary>
        /// Loại hình tính tiền: 0=Unspecified, 1=ConsumptionBased, 2=KitchenPrepared
        /// </summary>
        public int? BillingType { get; set; }
        
        /// <summary>
        /// Trạng thái món trong bếp: Pending, Cooking, Done, Served, Removed
        /// </summary>
        public string? KitchenStatus { get; set; }
        
        /// <summary>
        /// Món có thể hủy không (chỉ Kitchen items ở NotStarted)
        /// </summary>
        public bool CanCancel { get; set; }
        
        /// <summary>
        /// True nếu là món chế biến trong bếp (BillingType = 2)
        /// </summary>
        public bool IsKitchenItem => BillingType == 2;
        
        /// <summary>
        /// True nếu là món tính theo tiêu hao (BillingType = 1)
        /// </summary>
        public bool IsConsumptionItem => BillingType == 1;

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

