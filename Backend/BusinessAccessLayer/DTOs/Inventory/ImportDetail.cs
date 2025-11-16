using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.DTOs.Inventory
{
    public class ImportDetail
    {
        public int Id { get; set; }
        public int ImportOrderId { get; set; }
        public int? IngredientId { get; set; }
        public string IngredientCode { get; set; } = null!;
        public string IngredientName { get; set; } = null!;
        public string Unit { get; set; } = null!;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public int WarehouseId { get; set; }
        public decimal TotalPrice { get; set; }
    }
}
