using System;
using System.Collections.Generic;

namespace DomainAccessLayer.Models;

public partial class Staff
{
    public int StaffId { get; set; }

    public int UserId { get; set; }

    public DateOnly HireDate { get; set; }

    public decimal SalaryBase { get; set; }
    public int Status { get; set; } // add property status

    public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();

    public virtual ICollection<Payroll> Payrolls { get; set; } = new List<Payroll>();

    public virtual ICollection<Shift> Shifts { get; set; } = new List<Shift>();

    public virtual User User { get; set; } = null!;

    public virtual ICollection<Position> Positions { get; set; } = new List<Position>();
}
