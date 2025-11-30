using System;
using System.Collections.Generic;

namespace DomainAccessLayer.Models;

public partial class ShiftTemplate
{
    public int Id { get; set; }
    public int DayTypeId { get; set; }
    public int DepartmentId { get; set; }
    public string Code { get; set; } = null!;
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public int RequiredEmployees { get; set; }

    public virtual DayType DayType { get; set; } = null!;
    public virtual Department Department { get; set; } = null!;
    public virtual ICollection<Shift> Shifts { get; set; } = new List<Shift>();
}
