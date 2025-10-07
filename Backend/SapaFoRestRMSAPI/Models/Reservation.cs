using System;
using System.Collections.Generic;

namespace SapaFoRestRMSAPI.Models;

public partial class Reservation
{
    public int ReservationId { get; set; }

    public int CustomerId { get; set; }

    public DateTime ReservationTime { get; set; }

    public int NumberOfGuests { get; set; }

    public string? Status { get; set; }

    public string? Notes { get; set; }

    public virtual Customer Customer { get; set; } = null!;

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<ReservationTable> ReservationTables { get; set; } = new List<ReservationTable>();
}
