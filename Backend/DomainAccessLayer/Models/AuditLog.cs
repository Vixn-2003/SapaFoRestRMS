using System;

namespace DomainAccessLayer.Models;

/// <summary>
/// Model đại diện cho audit log của payment events
/// </summary>
public partial class AuditLog
{
    public int AuditLogId { get; set; }

    /// <summary>
    /// Loại event: attempt_underpaid, payment_failed, payment_success, etc.
    /// </summary>
    public string EventType { get; set; } = null!;

    /// <summary>
    /// Entity liên quan: Order, Transaction, Payment
    /// </summary>
    public string EntityType { get; set; } = null!;

    /// <summary>
    /// ID của entity
    /// </summary>
    public int EntityId { get; set; }

    /// <summary>
    /// Mô tả chi tiết
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Dữ liệu JSON bổ sung
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// User ID thực hiện action (nếu có)
    /// </summary>
    public int? UserId { get; set; }

    /// <summary>
    /// IP address
    /// </summary>
    public string? IpAddress { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual User? User { get; set; }
}

