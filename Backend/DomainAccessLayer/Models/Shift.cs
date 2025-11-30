using System;

namespace DomainAccessLayer.Models;

public partial class Shift
{
    public int Id { get; set; }
    public DateTime Date { get; set; }

    public int TemplateId { get; set; }
    public int DepartmentId { get; set; }

    public string Code { get; set; } = null!;
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }

    public int RequiredEmployees { get; set; }

    public virtual ShiftTemplate Template { get; set; } = null!;
    public virtual Department Department { get; set; } = null!;
    public virtual ICollection<ShiftAssignment> ShiftAssignments { get; set; } = new List<ShiftAssignment>();
}
