using System;
using System.Collections.Generic;

namespace DomainAccessLayer.Models;

public partial class StockTransaction
{
    public int TransactionId { get; set; }

    public int IngredientId { get; set; }

    public string Type { get; set; } = null!;

    public decimal Quantity { get; set; }

    public DateTime? TransactionDate { get; set; }

    public string? Note { get; set; }

    public int? BatchId { get; set; }

    public virtual InventoryBatch? Batch { get; set; }

    public virtual Ingredient Ingredient { get; set; } = null!;
}
