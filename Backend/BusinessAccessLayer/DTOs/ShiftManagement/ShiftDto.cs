using System;
using System.Collections.Generic;

namespace BusinessAccessLayer.DTOs.ShiftManagement;

public class ShiftDto
{
    public int ShiftId { get; set; }
    public int StaffId { get; set; }
    public string StaffName { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public DateOnly Date { get; set; }
    public decimal? OpeningBalance { get; set; }
    public decimal? ClosingBalance { get; set; }
    public string Status { get; set; }
    public decimal? Difference { get; set; }
    public string Notes { get; set; }
    public int? HandoverToStaffId { get; set; }
    public string HandoverToStaffName { get; set; }
    public string HandoverNotes { get; set; }
    public DateTime? HandoverTime { get; set; }

    // Calculated fields
    public decimal TotalRevenue { get; set; }
    public decimal SystemCash { get; set; }
    public decimal SystemCard { get; set; }
    public decimal SystemQR { get; set; }
    public int TotalOrders { get; set; }
    public int PendingOrders { get; set; }
    public decimal Discount { get; set; }
    public decimal ServiceFee { get; set; }
    public decimal Vat { get; set; }
    public decimal Debt { get; set; }
    public decimal TotalItems { get; set; }
}

public class OpenShiftRequestDto
{
    public int StaffId { get; set; }
    public decimal OpeningBalance { get; set; }
    public Dictionary<int, int> Denominations { get; set; }
}

public class CloseShiftRequestDto
{
    public int ShiftId { get; set; }
    public decimal ClosingBalance { get; set; }
    public Dictionary<int, int> Denominations { get; set; }
    public decimal Difference { get; set; }
    public string Notes { get; set; }
}

public class HandoverShiftRequestDto
{
    public int ShiftId { get; set; }
    public int HandoverToStaffId { get; set; }
    public string Notes { get; set; }
    public string PinCode { get; set; }
}

public class ShiftDashboardDto
{
    public int ShiftId { get; set; }
    public string Id { get; set; } // CA20241127-001
    public string Cashier { get; set; }
    public string StartTime { get; set; }
    public string CurrentTime { get; set; }
    public string StartDate { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal SystemCash { get; set; }
    public decimal SystemCard { get; set; }
    public decimal SystemQR { get; set; }
    public decimal TotalRevenue { get; set; }
    public int TotalOrders { get; set; }
    public int PendingOrders { get; set; }
    public decimal Discount { get; set; }
    public decimal ServiceFee { get; set; }
    public decimal Vat { get; set; }
    public decimal Debt { get; set; }
    public decimal TotalItems { get; set; }
    public string Status { get; set; }
}

public class ShiftResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public ShiftDashboardDto Data { get; set; }
}

