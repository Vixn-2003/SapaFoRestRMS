using System;
using System.ComponentModel.DataAnnotations;

namespace BusinessAccessLayer.DTOs.Positions;

/// <summary>
/// DTO cho yêu cầu thay đổi lương
/// </summary>
public class SalaryChangeRequestDto
{
    public int RequestId { get; set; }

    public int PositionId { get; set; }

    public string PositionName { get; set; } = null!;

    public decimal CurrentBaseSalary { get; set; }

    public decimal ProposedBaseSalary { get; set; }

    public string? Reason { get; set; }

    public string Status { get; set; } = null!;

    public int RequestedBy { get; set; }

    public string RequestedByName { get; set; } = null!;

    public int? ApprovedBy { get; set; }

    public string? ApprovedByName { get; set; }

    public string? OwnerNotes { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ReviewedAt { get; set; }
}

/// <summary>
/// DTO cho request tạo yêu cầu thay đổi lương (Manager)
/// Manager không thể trực tiếp update BaseSalary, phải tạo yêu cầu và chờ Owner phê duyệt
/// </summary>
public class CreateSalaryChangeRequestDto
{
    [Required(ErrorMessage = "PositionId là bắt buộc")]
    public int PositionId { get; set; }

    [Required(ErrorMessage = "ProposedBaseSalary là bắt buộc")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Lương đề xuất phải lớn hơn 0")]
    public decimal ProposedBaseSalary { get; set; }

    [MaxLength(500, ErrorMessage = "Lý do không được vượt quá 500 ký tự")]
    public string? Reason { get; set; }
}

/// <summary>
/// DTO cho request phê duyệt/từ chối (Owner)
/// </summary>
public class ReviewSalaryChangeRequestDto
{
    [Required(ErrorMessage = "RequestId là bắt buộc")]
    public int RequestId { get; set; }

    [Required(ErrorMessage = "Action là bắt buộc (Approve hoặc Reject)")]
    public string Action { get; set; } = null!; // "Approve" hoặc "Reject"

    [MaxLength(500, ErrorMessage = "Ghi chú không được vượt quá 500 ký tự")]
    public string? OwnerNotes { get; set; }
}

/// <summary>
/// DTO cho thống kê yêu cầu thay đổi lương
/// </summary>
public class SalaryChangeRequestStatisticsDto
{
    public int TotalRequests { get; set; }

    public int PendingRequests { get; set; }

    public int ApprovedRequests { get; set; }

    public int RejectedRequests { get; set; }

    public decimal TotalProposedIncrease { get; set; }

    public decimal AverageProposedIncrease { get; set; }
}

