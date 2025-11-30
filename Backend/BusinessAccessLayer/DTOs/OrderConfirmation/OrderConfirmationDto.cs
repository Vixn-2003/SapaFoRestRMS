namespace BusinessAccessLayer.DTOs.OrderConfirmation
{
    /// <summary>
    /// DTO cho màn hình xác nhận hóa đơn
    /// Chia rõ 2 loại món: Kitchen-prepared và Consumption-based
    /// </summary>
    public class OrderConfirmationDto
    {
        public int OrderId { get; set; }
        
        public string OrderCode { get; set; } = string.Empty;
        
        public string? TableNumber { get; set; }
        
        public List<string>? TableNumbers { get; set; }
        
        public string? CustomerName { get; set; }
        
        public string? CustomerPhone { get; set; }
        
        public DateTime? CreatedAt { get; set; }
        
        public string Status { get; set; } = string.Empty;
        
        /// <summary>
        /// Tất cả món trong đơn
        /// </summary>
        public List<OrderItemConfirmDto> AllItems { get; set; } = new();
        
        /// <summary>
        /// Món chế biến trong bếp
        /// </summary>
        public List<OrderItemConfirmDto> KitchenItems => 
            AllItems.Where(x => x.IsKitchenItem && x.Status != "Removed").ToList();
        
        /// <summary>
        /// Món tính theo số lượng sử dụng
        /// </summary>
        public List<OrderItemConfirmDto> ConsumptionItems => 
            AllItems.Where(x => x.IsConsumptionItem && x.Status != "Removed").ToList();
        
        /// <summary>
        /// Tổng tiền món bếp
        /// </summary>
        public decimal KitchenItemsSubtotal => KitchenItems.Sum(x => x.TotalPrice);
        
        /// <summary>
        /// Tổng tiền món tiêu hao
        /// </summary>
        public decimal ConsumptionItemsSubtotal => ConsumptionItems.Sum(x => x.TotalPrice);
        
        /// <summary>
        /// Tạm tính (trước VAT & phí dịch vụ)
        /// </summary>
        public decimal Subtotal => KitchenItemsSubtotal + ConsumptionItemsSubtotal;
        
        /// <summary>
        /// VAT (10%)
        /// </summary>
        public decimal VatAmount => Subtotal * 0.1m;
        
        /// <summary>
        /// Phí dịch vụ (5%)
        /// </summary>
        public decimal ServiceFee => Subtotal * 0.05m;
        
        /// <summary>
        /// Giảm giá (nếu có)
        /// </summary>
        public decimal DiscountAmount { get; set; }
        
        /// <summary>
        /// Tổng cộng thanh toán
        /// </summary>
        public decimal TotalAmount => Subtotal + VatAmount + ServiceFee - DiscountAmount;
        
        /// <summary>
        /// Số món không thể hủy (đang hoặc đã chế biến)
        /// </summary>
        public int NonCancelableItemsCount => 
            KitchenItems.Count(x => !x.CanCancel);
        
        /// <summary>
        /// Ghi chú của đơn
        /// </summary>
        public string? Notes { get; set; }
    }
}

