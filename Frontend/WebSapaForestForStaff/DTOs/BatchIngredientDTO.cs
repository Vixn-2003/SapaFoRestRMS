namespace WebSapaForestForStaff.DTOs
{
    public class BatchIngredientDTO
    {
        public int BatchId { get; set; }
        public decimal QuantityRemaining { get; set; }
        public DateOnly? ExpiryDate { get; set; }
        public DateTime? CreatedAt { get; set; }

        // Thông tin nguyên liệu
        public int IngredientId { get; set; }
        public string IngredientName { get; set; } = string.Empty;
        public string? IngredientUnit { get; set; }

        // Thông tin đơn hàng
        public int? PurchaseOrderId { get; set; }
        public DateTime? OrderDate { get; set; }
        public string? OrderStatus { get; set; }

        // Thông tin nhà cung cấp
        public int? SupplierId { get; set; }
        public string? SupplierName { get; set; }
        public string? SupplierCode { get; set; }
        public string? SupplierPhone { get; set; }
    }
}
