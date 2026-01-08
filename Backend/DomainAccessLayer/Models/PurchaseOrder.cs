using System;
using System.Collections.Generic;

namespace DomainAccessLayer.Models;

public partial class PurchaseOrder
{
    public string PurchaseOrderId { get; set; } = null!; // Đổi từ int sang string
    public int SupplierId { get; set; }
    public DateTime? OrderDate { get; set; }
    public DateTime? TimeConfirm { get; set; }
    public string? Status { get; set; }
    public int? IdCreator { get; set; }
    public int? IdConfirm { get; set; }
    public string? UrlImg { get; set; }
    public virtual User? Creator { get; set; }
    public virtual User? Confirmer { get; set; }
    public virtual Supplier Supplier { get; set; } = null!;
    public virtual ICollection<PurchaseOrderDetail> PurchaseOrderDetails { get; set; } = new List<PurchaseOrderDetail>();
}
