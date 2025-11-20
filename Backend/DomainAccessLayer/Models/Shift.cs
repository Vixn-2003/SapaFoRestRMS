using System;

namespace DomainAccessLayer.Models;

public partial class Shift
{
    public int ShiftId { get; set; }

    public int StaffId { get; set; }

    public int DepartmentId { get; set; }      // Bộ phận của ca

    public DateTime StartTime { get; set; }    // 2025-11-20 08:00

    public DateTime EndTime { get; set; }      // 2025-11-20 16:00

    public string ShiftType { get; set; } = null!; // Sáng / Chiều / Tối / Full

    public int? TemplateId { get; set; }       // Nếu ca được tạo từ template

    public int? RecurringId { get; set; }      // Nếu ca được sinh từ ca lặp

    public string? Note { get; set; }          // Ghi chú

    public int Status { get; set; }            // Active / Cancelled / Completed

    public virtual Staff Staff { get; set; } = null!;

    public virtual Department Department { get; set; } = null!;

    public virtual ShiftTemplate? Template { get; set; }

    public virtual WeeklyRecurringShift? RecurringShift { get; set; }

    public virtual ICollection<ShiftHistory> ShiftHistories { get; set; } = new List<ShiftHistory>();
}
