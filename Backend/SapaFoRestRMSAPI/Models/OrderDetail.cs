using System;
using System.Collections.Generic;

namespace SapaFoRestRMSAPI.Models;

public partial class OrderDetail
{
    public int OrderDetailId { get; set; }

    public int OrderId { get; set; }

    public int MenuItemId { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public string? Status { get; set; }

    public virtual ICollection<KitchenTicketDetail> KitchenTicketDetails { get; set; } = new List<KitchenTicketDetail>();

    public virtual MenuItem MenuItem { get; set; } = null!;

    public virtual Order Order { get; set; } = null!;
}
