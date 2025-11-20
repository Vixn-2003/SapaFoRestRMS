using System;
using System.Collections.Generic;

namespace DomainAccessLayer.Models;

public partial class WeeklyRecurringShift
{
    public int WeeklyRecurringShiftId { get; set; }

    public int StaffId { get; set; }

    public int TemplateId { get; set; }

    public string DaysOfWeek { get; set; } = null!;
    // Ví dụ: "Mon,Wed,Fri"

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public virtual Staff Staff { get; set; } = null!;

    public virtual ShiftTemplate Template { get; set; } = null!;

    public virtual ICollection<Shift> Shifts { get; set; } = new List<Shift>();
}
