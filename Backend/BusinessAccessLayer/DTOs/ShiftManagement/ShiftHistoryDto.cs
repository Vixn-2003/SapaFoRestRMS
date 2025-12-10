using System;
using System.Collections.Generic;

namespace BusinessAccessLayer.DTOs.ShiftManagement;

/// <summary>
/// DTO cho ShiftHistory (audit trail)
/// </summary>
public class ShiftHistoryDto
{
    public int ShiftHistoryId { get; set; }
    public int ShiftId { get; set; }
    public string ShiftCode { get; set; } = string.Empty;
    public int ActionBy { get; set; } // UserId
    public string ActionByName { get; set; } = string.Empty; // Staff name
    public string Action { get; set; } = string.Empty; // "Open", "Close", "Handover", "Edit", etc.
    public DateTime ActionAt { get; set; }
    public string Detail { get; set; } = string.Empty; // JSON or text details
}

/// <summary>
/// DTO cho lịch sử chi tiết của một shift (UC137)
/// </summary>
public class ShiftDetailDto
{
    public int ShiftId { get; set; }
    public string ShiftCode { get; set; } = string.Empty;
    public int StaffId { get; set; }
    public string StaffName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string Status { get; set; } = string.Empty;

    // Financial info
    public decimal? OpeningBalance { get; set; }
    public decimal? ClosingBalance { get; set; }
    public decimal? Difference { get; set; }
    public string? Notes { get; set; }

    // Denominations (JSON serialized)
    public string? OpeningDenominations { get; set; }
    public string? ClosingDenominations { get; set; }

    // Revenue summary
    public decimal TotalRevenue { get; set; }
    public decimal TotalCash { get; set; }
    public decimal TotalCard { get; set; }
    public decimal TotalQR { get; set; }
    public int TotalOrders { get; set; }

    // Handover info
    public int? HandoverToStaffId { get; set; }
    public string? HandoverToStaffName { get; set; }
    public string? HandoverNotes { get; set; }
    public DateTime? HandoverTime { get; set; }

    // History trail
    public List<ShiftHistoryDto> Histories { get; set; } = new();
}

/// <summary>
/// Request DTO cho việc filter shift history (UC136)
/// </summary>
public class ShiftFilterDto
{
    public int? StaffId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? Status { get; set; } // "Open", "Closed", "Handover"
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

/// <summary>
/// Response DTO cho danh sách shift history (paginated)
/// </summary>
public class ShiftHistoryListDto
{
    public List<ShiftDto> Shifts { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

