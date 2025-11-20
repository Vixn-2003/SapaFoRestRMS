using System;
using System.Collections.Generic;

namespace DomainAccessLayer.Models;

public partial class InventoryBatch
{
    public int BatchId { get; set; }

    public int IngredientId { get; set; }

    public int? PurchaseOrderDetailId { get; set; }

    public int WarehouseId { get; set; }  // 🔥 Thêm cột này để liên kết kho chứa

    public decimal QuantityRemaining { get; set; }

    public DateOnly? ExpiryDate { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Ingredient Ingredient { get; set; } = null!;

    public virtual PurchaseOrderDetail? PurchaseOrderDetail { get; set; }

    public virtual Warehouse Warehouse { get; set; } = null!; // 🔥 Liên kết tới bảng Warehouse

    public virtual ICollection<StockTransaction> StockTransactions { get; set; } = new List<StockTransaction>();
}
