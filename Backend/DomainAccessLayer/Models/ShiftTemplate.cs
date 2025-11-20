using System;
using System.Collections.Generic;

namespace DomainAccessLayer.Models;

public partial class ShiftTemplate
{
    public int ShiftTemplateId { get; set; }

    public string Name { get; set; } = null!;      // Ví dụ: "Ca Sáng", "Ca Tối"

    public TimeOnly Start { get; set; }            // 08:00
    public TimeOnly End { get; set; }              // 16:00

    public string ShiftType { get; set; } = null!; // Sáng / Chiều / Tối / Full

    public int DepartmentId { get; set; }

    public virtual Department Department { get; set; } = null!;

    public virtual ICollection<Shift> Shifts { get; set; } = new List<Shift>();

    public virtual ICollection<WeeklyRecurringShift> WeeklyRecurringShifts { get; set; } = new List<WeeklyRecurringShift>();
}
