using System;
using System.Collections.Generic;

namespace WebSapaForestForStaff.Models;

public partial class InventoryBatch
{
    public int BatchId { get; set; }

    public int IngredientId { get; set; }

    public int? PurchaseOrderDetailId { get; set; }

    public decimal QuantityRemaining { get; set; }

    public DateOnly? ExpiryDate { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Ingredient Ingredient { get; set; } = null!;

    public virtual PurchaseOrderDetail? PurchaseOrderDetail { get; set; }

    public virtual ICollection<StockTransaction> StockTransactions { get; set; } = new List<StockTransaction>();
}
