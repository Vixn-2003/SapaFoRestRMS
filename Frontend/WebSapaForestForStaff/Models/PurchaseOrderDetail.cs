using System;
using System.Collections.Generic;

namespace WebSapaForestForStaff.Models;

public partial class PurchaseOrderDetail
{
    public int PurchaseOrderDetailId { get; set; }

    public int PurchaseOrderId { get; set; }

    public int IngredientId { get; set; }

    public decimal Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public virtual Ingredient Ingredient { get; set; } = null!;

    public virtual ICollection<InventoryBatch> InventoryBatches { get; set; } = new List<InventoryBatch>();

    public virtual PurchaseOrder PurchaseOrder { get; set; } = null!;
}
