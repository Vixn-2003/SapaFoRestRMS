using System;
using System.Collections.Generic;

namespace WebSapaForestForStaff.DTOs.ShiftManagement
{
    /// <summary>
    /// Full Shift DTO matching backend
    /// </summary>
    public class ShiftDto
    {
        public int ShiftId { get; set; }
        public int StaffId { get; set; }
        public string StaffName { get; set; } = string.Empty;
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public DateTime Date { get; set; }
        public decimal? OpeningBalance { get; set; }
        public decimal? ClosingBalance { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal? Difference { get; set; }
        public string? Notes { get; set; }
        public int? HandoverToStaffId { get; set; }
        public string? HandoverToStaffName { get; set; }
        public string? HandoverNotes { get; set; }
        public DateTime? HandoverTime { get; set; }
    }

    /// <summary>
    /// Opening DTOs
    /// </summary>
    public class ShiftOpeningDeclareRequestDto
    {
        public int StaffId { get; set; }
        public decimal OpeningBalance { get; set; }
        public string? Notes { get; set; }
    }

    public class ShiftOpeningDenominationsRequestDto
    {
        public int ShiftId { get; set; }
        public List<ShiftDenominationDto> Denominations { get; set; } = new();
    }

    public class ShiftOpeningConfirmRequestDto
    {
        public int ShiftId { get; set; }
        public int StaffId { get; set; }
        public decimal OpeningBalance { get; set; }
        public string? OpeningDenominations { get; set; }
        public string? Notes { get; set; }
    }

    public class ShiftOpeningResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public ShiftDto? Shift { get; set; }
    }

    /// <summary>
    /// Denomination DTO
    /// </summary>
    public class ShiftDenominationDto
    {
        public int Denomination { get; set; } // 1000, 2000, 5000, etc.
        public int Count { get; set; }
        public decimal Total => Denomination * Count;
    }

    public class ShiftDenominationsRequestDto
    {
        public int ShiftId { get; set; }
        public List<ShiftDenominationDto> Denominations { get; set; } = new();
        public decimal TotalAmount { get; set; }
    }

    public class ShiftDenominationsResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<ShiftDenominationDto> Denominations { get; set; } = new();
        public decimal TotalAmount { get; set; }
    }

    /// <summary>
    /// Closing DTOs
    /// </summary>
    public class ShiftClosingDto
    {
        public int ShiftId { get; set; }
        public decimal OpeningBalance { get; set; }
        public decimal ExpectedCash { get; set; }
        public decimal ClosingBalance { get; set; }
        public decimal Difference { get; set; }
        public string Notes { get; set; } = string.Empty;
        public DateTime? ClosedAt { get; set; }
    }

    public class ShiftDifferenceDto
    {
        public decimal OpeningBalance { get; set; }
        public decimal TotalRevenueCash { get; set; }
        public decimal ExpectedClosingBalance { get; set; }
        public decimal ActualClosingBalance { get; set; }
        public decimal Difference { get; set; }
        public bool HasDifference => Math.Abs(Difference) > 0;
        public string DifferenceType => Difference > 0 ? "Surplus" : Difference < 0 ? "Shortage" : "Balanced";
    }

    public class ShiftClosingNotesRequestDto
    {
        public int ShiftId { get; set; }
        public string Notes { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }

    public class ShiftClosingConfirmRequestDto
    {
        public int ShiftId { get; set; }
        public decimal ClosingBalance { get; set; }
        public string Notes { get; set; } = string.Empty;
        public int StaffId { get; set; }
    }

    /// <summary>
    /// Handover DTOs
    /// </summary>
    public class ShiftHandoverDto
    {
        public int ShiftId { get; set; }
        public int FromStaffId { get; set; }
        public string FromStaffName { get; set; } = string.Empty;
        public int ToStaffId { get; set; }
        public string ToStaffName { get; set; } = string.Empty;
        public decimal ClosingBalance { get; set; }
        public string HandoverNotes { get; set; } = string.Empty;
        public DateTime HandoverTime { get; set; }
    }

    public class ShiftHandoverSelectStaffRequestDto
    {
        public int ShiftId { get; set; }
        public int HandoverToStaffId { get; set; }
    }

    public class ShiftHandoverNotesRequestDto
    {
        public int ShiftId { get; set; }
        public string HandoverNotes { get; set; } = string.Empty;
    }

    public class ShiftHandoverPinRequestDto
    {
        public int ShiftId { get; set; }
        public int FromStaffId { get; set; }
        public string PinCode { get; set; } = string.Empty;
    }

    public class ShiftHandoverCreateNextRequestDto
    {
        public int CurrentShiftId { get; set; }
        public int FromStaffId { get; set; }
        public int ToStaffId { get; set; }
        public string HandoverNotes { get; set; } = string.Empty;
        public string PinCode { get; set; } = string.Empty;
        public decimal ClosingBalance { get; set; }
    }

    public class ShiftHandoverResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public ShiftDto? CurrentShift { get; set; }
        public ShiftDto? NextShift { get; set; }
    }

    public class ShiftStaffDto
    {
        public int StaffId { get; set; }
        public string StaffName { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public bool IsAvailable { get; set; } = true;
        public string? CurrentShiftStatus { get; set; }
    }

    /// <summary>
    /// History & Filter DTOs
    /// </summary>
    public class ShiftHistoryDto
    {
        public int ShiftHistoryId { get; set; }
        public int ShiftId { get; set; }
        public string ShiftCode { get; set; } = string.Empty;
        public int ActionBy { get; set; }
        public string ActionByName { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public DateTime ActionAt { get; set; }
        public string Detail { get; set; } = string.Empty;
    }

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

        public decimal? OpeningBalance { get; set; }
        public decimal? ClosingBalance { get; set; }
        public decimal? Difference { get; set; }
        public string? Notes { get; set; }

        public string? OpeningDenominations { get; set; }
        public string? ClosingDenominations { get; set; }

        public decimal TotalRevenue { get; set; }
        public decimal TotalCash { get; set; }
        public decimal TotalCard { get; set; }
        public decimal TotalQR { get; set; }
        public int TotalOrders { get; set; }

        public int? HandoverToStaffId { get; set; }
        public string? HandoverToStaffName { get; set; }
        public string? HandoverNotes { get; set; }
        public DateTime? HandoverTime { get; set; }

        public List<ShiftHistoryDto> Histories { get; set; } = new();
    }

    public class ShiftFilterDto
    {
        public int? StaffId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? Status { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class ShiftHistoryListDto
    {
        public List<ShiftDto> Shifts { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }
}

