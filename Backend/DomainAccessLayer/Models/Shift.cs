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

    // Extended properties for Shift Management
    public decimal? OpeningBalance { get; set; }
    public decimal? ClosingBalance { get; set; }
    public string? OpeningDenominations { get; set; } // JSON string
    public string? ClosingDenominations { get; set; } // JSON string
    public string? Status { get; set; } // "Open", "Closed", "Handover"
    public decimal? Difference { get; set; } // Chênh lệch giữa hệ thống và thực tế
    public string? Notes { get; set; }
    public int? HandoverToStaffId { get; set; }
    public string? HandoverNotes { get; set; }
    public DateTime? HandoverTime { get; set; }
    public string? PinCode { get; set; } // Mã PIN xác nhận (encrypted)
    public int? StaffId { get; set; }

    // Navigation properties
    public virtual Staff Staff { get; set; } = null!;
    public virtual ShiftTemplate Template { get; set; } = null!;
    public virtual Department Department { get; set; } = null!;
    public virtual ICollection<ShiftAssignment> ShiftAssignments { get; set; } = new List<ShiftAssignment>();
}
