using System;
using System.Collections.Generic;

namespace DomainAccessLayer.Models;

public partial class KitchenTicketDetail
{
    public int TicketDetailId { get; set; }

    public int TicketId { get; set; }

    public int OrderDetailId { get; set; }

    public virtual OrderDetail OrderDetail { get; set; } = null!;

    public virtual KitchenTicket Ticket { get; set; } = null!;
}
