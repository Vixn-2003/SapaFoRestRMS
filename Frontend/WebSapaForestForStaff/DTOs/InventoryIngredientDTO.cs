using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebSapaForestForStaff.DTOs.Inventory;

namespace BusinessAccessLayer.DTOs.Inventory
{
    public class InventoryIngredientDTO
    {
        public int IngredientId { get; set; }
        public string IngredientCode { get; set; } = string.Empty!;
        public string Name { get; set; } = null!;
        public int? UnitId { get; set; }
        public decimal? ReorderLevel { get; set; }

        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? SelectedUnit { get; set; }
        public string? SearchIngredent { get; set; }

        public List<InventoryBatchDTO> Batches { get; set; } = new();
        public UnitDTO Unit { get; set; } = new();

        public decimal TotalQuantity => Batches.Sum(b => b.QuantityRemaining);

        public decimal TotalExport { get; set; }
        public decimal TotalImport { get; set; }
        public decimal OriginalQuantity { get; set; }
    }
}
