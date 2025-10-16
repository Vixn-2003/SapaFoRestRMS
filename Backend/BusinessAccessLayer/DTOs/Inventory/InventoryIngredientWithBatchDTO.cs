using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.DTOs.Inventory
{
    public class InventoryIngredientWithBatchDTO
    {
        public int IngredientId { get; set; }
        public string Name { get; set; } = null!;
        public string? Unit { get; set; }
        public decimal? ReorderLevel { get; set; }

        // Thông tin batch
        public int BatchId { get; set; }
        public decimal QuantityRemaining { get; set; }
        public DateOnly? ExpiryDate { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
