using System;
using System.Collections.Generic;

namespace DomainAccessLayer.Models;

public partial class ReservationTable
{
    public int ReservationTableId { get; set; }

    public int ReservationId { get; set; }

    public int TableId { get; set; }

    public virtual Reservation Reservation { get; set; } = null!;

    public virtual Table Table { get; set; } = null!;
}
