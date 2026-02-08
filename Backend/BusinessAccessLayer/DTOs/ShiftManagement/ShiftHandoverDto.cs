using System;
using System.Collections.Generic;

namespace BusinessAccessLayer.DTOs.ShiftManagement;

/// <summary>
/// Request DTO cho UC132 - Select Handover Staff
/// </summary>
public class ShiftHandoverSelectStaffRequestDto
{
    public int ShiftId { get; set; }
    public int HandoverToStaffId { get; set; }
}

/// <summary>
/// Request DTO cho UC133 - Add Handover Notes
/// </summary>
public class ShiftHandoverNotesRequestDto
{
    public int ShiftId { get; set; }
    public string HandoverNotes { get; set; } = string.Empty;
}

/// <summary>
/// Request DTO cho UC134 - Enter PIN Code
/// </summary>
public class ShiftHandoverPinRequestDto
{
    public int ShiftId { get; set; }
    public int FromStaffId { get; set; } // Staff who is handing over
    public string PinCode { get; set; } = string.Empty; // PIN for verification
}

/// <summary>
/// Request DTO cho UC135 - Create Next Shift (sau khi handover)
/// </summary>
public class ShiftHandoverCreateNextRequestDto
{
    public int CurrentShiftId { get; set; }
    public int FromStaffId { get; set; }
    public int ToStaffId { get; set; }
    public string HandoverNotes { get; set; } = string.Empty;
    public string PinCode { get; set; } = string.Empty;
    public decimal ClosingBalance { get; set; } // From current shift
}

/// <summary>
/// Response DTO cho handover operation
/// </summary>
public class ShiftHandoverResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public ShiftDto? CurrentShift { get; set; } // Closed shift
    public ShiftDto? NextShift { get; set; } // Newly created shift for next staff
}

/// <summary>
/// DTO cho danh sách staff có thể nhận ca
/// </summary>
public class ShiftStaffDto
{
    public int StaffId { get; set; }
    public string StaffName { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public bool IsAvailable { get; set; } = true;
    public string? CurrentShiftStatus { get; set; } // "Open", "Closed", null
}

