using System;
using System.Collections.Generic;

namespace DomainAccessLayer.Models;

public partial class Table
{
    public int TableId { get; set; }

    public string TableNumber { get; set; } = null!;

    public int Capacity { get; set; }

    public string? Status { get; set; }
    public int AreaId { get; set; }

    // Navigation property
    public virtual Area Area { get; set; } = null!;
    public virtual ICollection<ReservationTable> ReservationTables { get; set; } = new List<ReservationTable>();
}
