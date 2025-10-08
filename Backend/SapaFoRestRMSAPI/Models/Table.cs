using System;
using System.Collections.Generic;

namespace SapaFoRestRMSAPI.Models;

public partial class Table
{
    public int TableId { get; set; }

    public string TableNumber { get; set; } = null!;

    public int Capacity { get; set; }

    public string? Status { get; set; }

    public virtual ICollection<ReservationTable> ReservationTables { get; set; } = new List<ReservationTable>();
}
