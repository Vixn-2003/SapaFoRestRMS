using System;
using System.Collections.Generic;

namespace DomainAccessLayer.Models;

public partial class PurchaseOrderDetail
{
    public int PurchaseOrderDetailId { get; set; }

    public string PurchaseOrderId { get; set; } = null!;

    public int? IngredientId { get; set; }

    public string? IngredientCode { get; set; }
    public string? IngredientName { get; set; }
    public string? Unit { get; set; }

    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }

    public decimal Subtotal { get; set; }

    public string? WarehouseName { get; set; }

    public virtual PurchaseOrder PurchaseOrder { get; set; } = null!;
    public virtual Ingredient? Ingredient { get; set; }

    public virtual ICollection<InventoryBatch> InventoryBatches { get; set; } = new List<InventoryBatch>();
}
