using System;

namespace BusinessAccessLayer.DTOs.ShiftManagement;

/// <summary>
/// DTO cho thông tin closing shift
/// </summary>
public class ShiftClosingDto
{
    public int ShiftId { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal ExpectedCash { get; set; } // Expected cash from transactions
    public decimal ClosingBalance { get; set; } // Actual cash counted
    public decimal Difference { get; set; } // ClosingBalance - (OpeningBalance + ExpectedCash)
    public string Notes { get; set; } = string.Empty;
    public DateTime? ClosedAt { get; set; }
}

/// <summary>
/// DTO cho việc tính toán chênh lệch (UC129 - Calculate Difference)
/// </summary>
public class ShiftDifferenceDto
{
    public decimal OpeningBalance { get; set; }
    public decimal TotalRevenueCash { get; set; } // Revenue from Cash payments
    public decimal ExpectedClosingBalance { get; set; } // OpeningBalance + TotalRevenueCash
    public decimal ActualClosingBalance { get; set; } // Actual amount counted
    public decimal Difference { get; set; } // Actual - Expected
    public bool HasDifference => Math.Abs(Difference) > 0;
    public string DifferenceType => Difference > 0 ? "Surplus" : Difference < 0 ? "Shortage" : "Balanced";
}

/// <summary>
/// Request DTO cho việc add notes khi closing (UC130)
/// </summary>
public class ShiftClosingNotesRequestDto
{
    public int ShiftId { get; set; }
    public string Notes { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty; // Lý do chênh lệch (nếu có)
}

/// <summary>
/// Request DTO cho việc confirm closing shift (UC131)
/// </summary>
public class ShiftClosingConfirmRequestDto
{
    public int ShiftId { get; set; }
    public decimal ClosingBalance { get; set; }
    public string Notes { get; set; } = string.Empty;
    public int StaffId { get; set; } // Staff performing the closing
}

