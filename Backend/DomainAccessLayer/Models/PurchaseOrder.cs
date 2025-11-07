using System;
using System.Collections.Generic;

namespace DomainAccessLayer.Models;

public partial class PurchaseOrder
{
    public int PurchaseOrderId { get; set; }

    public int SupplierId { get; set; }

    public DateTime? OrderDate { get; set; }

    public string? Status { get; set; }

    public int? IdCreator { get; set; }
    public int? IdConfirm { get; set; }

    public string? UrlImg { get; set; }
    public virtual User? Creator { get; set; }
    public virtual User? Confirmer { get; set; }

    public virtual Supplier Supplier { get; set; } = null!;
    public virtual ICollection<PurchaseOrderDetail> PurchaseOrderDetails { get; set; } = new List<PurchaseOrderDetail>();
}
