using System;

namespace DomainAccessLayer.Models;

/// <summary>
/// Model đại diện cho yêu cầu thay đổi lương cơ bản của Position
/// </summary>
public partial class SalaryChangeRequest
{
    public int RequestId { get; set; }

    public int PositionId { get; set; }

    /// <summary>
    /// Lương cơ bản hiện tại
    /// </summary>
    public decimal CurrentBaseSalary { get; set; }

    /// <summary>
    /// Lương cơ bản đề xuất (mới)
    /// </summary>
    public decimal ProposedBaseSalary { get; set; }

    /// <summary>
    /// Lý do thay đổi
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Trạng thái: Pending, Approved, Rejected
    /// </summary>
    public string Status { get; set; } = "Pending";

    /// <summary>
    /// Người tạo yêu cầu (Manager)
    /// </summary>
    public int RequestedBy { get; set; }

    /// <summary>
    /// Người phê duyệt (Owner)
    /// </summary>
    public int? ApprovedBy { get; set; }

    /// <summary>
    /// Ghi chú từ Owner khi phê duyệt/từ chối
    /// </summary>
    public string? OwnerNotes { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ReviewedAt { get; set; }

    // Navigation properties
    public virtual Position Position { get; set; } = null!;

    public virtual User RequestedByUser { get; set; } = null!;

    public virtual User? ApprovedByUser { get; set; }
}

