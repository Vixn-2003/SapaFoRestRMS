using System.Collections.Generic;

namespace BusinessAccessLayer.DTOs.ShiftManagement;

/// <summary>
/// DTO cho khai báo mệnh giá tiền (count denominations)
/// </summary>
public class ShiftDenominationDto
{
    public int Denomination { get; set; } // Mệnh giá: 1000, 2000, 5000, 10000, 20000, 50000, 100000, 200000, 500000
    public int Count { get; set; } // Số tờ
    public decimal Total => Denomination * Count;
}

/// <summary>
/// Request DTO cho việc submit denominations (opening/closing)
/// </summary>
public class ShiftDenominationsRequestDto
{
    public int ShiftId { get; set; }
    public List<ShiftDenominationDto> Denominations { get; set; } = new();
    public decimal TotalAmount { get; set; } // Tổng số tiền tính từ denominations
}

/// <summary>
/// Response DTO trả về danh sách denominations đã lưu
/// </summary>
public class ShiftDenominationsResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<ShiftDenominationDto> Denominations { get; set; } = new();
    public decimal TotalAmount { get; set; }
}

