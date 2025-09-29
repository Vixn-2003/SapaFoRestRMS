using System;
using System.Collections.Generic;

namespace DomainAccessLayer.Models;

public partial class Attendance
{
    public int AttendanceId { get; set; }

    public int StaffId { get; set; }

    public DateOnly Date { get; set; }

    public DateTime? CheckIn { get; set; }

    public DateTime? CheckOut { get; set; }

    public string? Status { get; set; }

    public string? Note { get; set; }

    public virtual Staff Staff { get; set; } = null!;
}
