using System;
using System.Collections.Generic;

namespace DomainAccessLayer.Models;

public partial class Order
{
    public int OrderId { get; set; }

    public int? ReservationId { get; set; }

    public int? CustomerId { get; set; }

    public string OrderType { get; set; } = null!;

    public decimal? TotalAmount { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Customer? Customer { get; set; }

    public virtual ICollection<KitchenTicket> KitchenTickets { get; set; } = new List<KitchenTicket>();

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();

    public virtual Reservation? Reservation { get; set; }
}
