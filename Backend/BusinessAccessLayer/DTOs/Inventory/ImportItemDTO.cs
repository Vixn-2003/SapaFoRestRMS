using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.DTOs.Inventory
{
    public class ImportItemDTO
    {
        public int? IngredientId { get; set; }
        public string IngredientCode { get; set; } = null!;
        public string IngredientName { get; set; } = null!;
        public string Unit { get; set; } = null!;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public string WarehouseName { get; set; }
        public decimal TotalPrice { get; set; }
    }
}
