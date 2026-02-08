using System;

namespace DomainAccessLayer.Models;

/// <summary>
/// Model để lock order khi đang xử lý thanh toán
/// </summary>
public partial class OrderLock
{
    public int OrderLockId { get; set; }

    public int OrderId { get; set; }

    /// <summary>
    /// User ID đang lock order
    /// </summary>
    public int LockedByUserId { get; set; }

    /// <summary>
    /// Session ID hoặc client identifier
    /// </summary>
    public string? SessionId { get; set; }

    /// <summary>
    /// Lý do lock
    /// </summary>
    public string Reason { get; set; } = "Payment in progress";

    public DateTime LockedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Thời gian hết hạn lock (mặc định 10 phút)
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    public virtual Order Order { get; set; } = null!;

    public virtual User LockedByUser { get; set; } = null!;
}

