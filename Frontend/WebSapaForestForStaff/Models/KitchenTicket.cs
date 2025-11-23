using System;
using System.Collections.Generic;

namespace WebSapaForestForStaff.Models;

public partial class KitchenTicket
{
    public int TicketId { get; set; }

    public int OrderId { get; set; }

    public string CourseType { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public string? Status { get; set; }

    public virtual ICollection<KitchenTicketDetail> KitchenTicketDetails { get; set; } = new List<KitchenTicketDetail>();

    public virtual Order Order { get; set; } = null!;
}

