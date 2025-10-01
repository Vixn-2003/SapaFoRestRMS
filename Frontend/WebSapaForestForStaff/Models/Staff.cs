using System;
using System.Collections.Generic;

namespace WebSapaForestForStaff.Models;

public partial class Staff
{
    public int StaffId { get; set; }

    public int UserId { get; set; }

    public string Position { get; set; } = null!;

    public DateOnly HireDate { get; set; }

    public decimal SalaryBase { get; set; }

    public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();

    public virtual ICollection<Payroll> Payrolls { get; set; } = new List<Payroll>();

    public virtual ICollection<Shift> Shifts { get; set; } = new List<Shift>();

    public virtual User User { get; set; } = null!;
}
