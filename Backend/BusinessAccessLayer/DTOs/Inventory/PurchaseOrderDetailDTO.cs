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

        public int PurchaseOrderId { get; set; }

        public int IngredientId { get; set; }

        public decimal Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public IngredientDTO Ingredient { get; set; }
    }
}
