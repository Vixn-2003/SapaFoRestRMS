using System;
using System.Collections.Generic;

namespace WebSapaForestForStaff.Models;

public partial class InventoryBatch
{
    public int BatchId { get; set; }

    public int IngredientId { get; set; }

    public int? PurchaseOrderDetailId { get; set; }

    public decimal QuantityRemaining { get; set; }

    public decimal QuantityReserved { get; set; } = 0; // Số lượng đã được bếp phó dành riêng

    public decimal Available { get; set; } // Số lượng khả dụng (computed column trong DB: QuantityRemaining - QuantityReserved)

    public DateOnly? ExpiryDate { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Ingredient Ingredient { get; set; } = null!;

    public virtual PurchaseOrderDetail? PurchaseOrderDetail { get; set; }

    public virtual ICollection<StockTransaction> StockTransactions { get; set; } = new List<StockTransaction>();
}
