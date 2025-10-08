using System;
using System.Collections.Generic;

namespace SapaFoRestRMSAPI.Models;

public partial class Customer
{
    public int CustomerId { get; set; }

    public int? UserId { get; set; }

    public int? LoyaltyPoints { get; set; }

    public string? Notes { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();

    public virtual User? User { get; set; }
}
