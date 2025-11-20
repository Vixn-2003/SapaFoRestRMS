using DomainAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.DTOs.Inventory
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

        public IngredientDTO Ingredient { get; set; }
    }
}
