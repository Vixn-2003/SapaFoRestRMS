using System;
using System.Collections.Generic;

namespace BusinessAccessLayer.DTOs.ShiftManagement;

/// <summary>
/// Request DTO cho UC125 - Declare Opening Balance
/// </summary>
public class ShiftOpeningDeclareRequestDto
{
    public int StaffId { get; set; }
    public decimal OpeningBalance { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Request DTO cho UC126 - Count Opening Denominations
/// </summary>
public class ShiftOpeningDenominationsRequestDto
{
    public int ShiftId { get; set; } // Temporary ShiftId (created after declare)
    public List<ShiftDenominationDto> Denominations { get; set; } = new();
}

/// <summary>
/// Request DTO cho UC127 - Confirm Shift Opening
/// </summary>
public class ShiftOpeningConfirmRequestDto
{
    public int ShiftId { get; set; }
    public int StaffId { get; set; }
    public decimal OpeningBalance { get; set; }
    public string? OpeningDenominations { get; set; } // JSON serialized
    public string? Notes { get; set; }
}

/// <summary>
/// Response DTO sau khi mở ca thành công
/// </summary>
public class ShiftOpeningResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public ShiftDto? Shift { get; set; }
}

