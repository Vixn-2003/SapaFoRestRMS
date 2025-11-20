using WebSapaForestForStaff.DTOs.Inventory;

namespace WebSapaForestForStaff.DTOs
{
    public class PurchaseOrderDetailDTO
    {
        public int PurchaseOrderDetailId { get; set; }

        public string PurchaseOrderId { get; set; } = null!;

        public int? IngredientId { get; set; }

        public string? IngredientCode { get; set; }
        public string? IngredientName { get; set; }
        public string? Unit { get; set; }

        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }

        public decimal Subtotal { get; set; }

        public string? WarehouseName { get; set; }

    }
}
