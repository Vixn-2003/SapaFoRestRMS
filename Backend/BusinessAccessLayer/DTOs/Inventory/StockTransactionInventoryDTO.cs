using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.DTOs.Inventory
{
    public class StockTransactionInventoryDTO
    {

        public int TransactionId { get; set; }
        public string Type { get; set; } = null!;
        public decimal Quantity { get; set; }
        public DateTime? TransactionDate { get; set; }
        public string? Note { get; set; }
        public int BatchId { get; set; }
        public decimal QuantityRemaining { get; set; }
        public DateOnly? ExpiryDate { get; set; }
        public DateTime? BatchCreatedAt { get; set; }

        public int IngredientId { get; set; }
        public string IngredientCode { get; set; } = null!;
        public string IngredientName { get; set; } = null!;
        public int UnitId { get; set; }
        public string UnitName { get; set; } = null!;
        public string UnitType { get; set; } = null!;

        public int WarehouseId { get; set; }
        public string WarehouseName { get; set; } = null!;

        public int? PurchaseOrderDetailId { get; set; }
        public string? PurchaseOrderId { get; set; }

        public string? PurchaseOrderStatus { get; set; }
        public DateTime? PurchaseOrderDate { get; set; }

        public string? SupplierName { get; set; }
        public string? SupplierCode { get; set; }
    }

}
