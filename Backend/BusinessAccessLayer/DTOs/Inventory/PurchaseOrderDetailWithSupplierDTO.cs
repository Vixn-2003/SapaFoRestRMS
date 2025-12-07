using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.DTOs.Inventory
{
    public class PurchaseOrderDetailWithSupplierDTO
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
        public DateOnly? ExpiryDate { get; set; }
        public string? WarehouseName { get; set; }

        // Thông tin từ PurchaseOrder
        public int SupplierId { get; set; }
        public DateTime? OrderDate { get; set; }
        public string? Status { get; set; }

        // Thông tin từ Supplier
        public string SupplierName { get; set; } = null!;
        public string SupplierCode { get; set; } = null!;
        public string? SupplierPhone { get; set; }
        public string? SupplierEmail { get; set; }
    }
}
