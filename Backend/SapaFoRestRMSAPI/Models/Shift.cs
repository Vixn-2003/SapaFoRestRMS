using System;
using System.Collections.Generic;

namespace SapaFoRestRMSAPI.Models;

public partial class Shift
{
    public int ShiftId { get; set; }

    public int StaffId { get; set; }

    public DateTime? StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    public DateOnly Date { get; set; }

    public virtual Staff Staff { get; set; } = null!;
}
