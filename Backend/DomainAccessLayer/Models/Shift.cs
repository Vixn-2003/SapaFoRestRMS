using System;
using System.Collections.Generic;

namespace DomainAccessLayer.Models;

public partial class Shift
{
    public int ShiftId { get; set; }

    public int StaffId { get; set; }

    public DateTime? StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    public DateOnly Date { get; set; }

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

    // Navigation properties
    public virtual Staff Staff { get; set; } = null!;
    public virtual Staff? HandoverToStaff { get; set; }
}
