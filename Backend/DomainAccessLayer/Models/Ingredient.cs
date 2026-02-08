using System;
using System.Collections.Generic;

namespace DomainAccessLayer.Models
{
    public partial class Ingredient
    {
        public int IngredientId { get; set; }

        public string IngredientCode { get; set; } = string.Empty!;

        public string Name { get; set; } = null!;

        public int UnitId { get; set; }

        public virtual Unit Unit { get; set; } = null!;

        public decimal? ReorderLevel { get; set; }

        public virtual ICollection<InventoryBatch> InventoryBatches { get; set; }
            = new List<InventoryBatch>();

        public virtual ICollection<PurchaseOrderDetail> PurchaseOrderDetails { get; set; }
            = new List<PurchaseOrderDetail>();

        public virtual ICollection<Recipe> Recipes { get; set; }
            = new List<Recipe>();

        public virtual ICollection<StockTransaction> StockTransactions { get; set; }
            = new List<StockTransaction>();
    }
}
