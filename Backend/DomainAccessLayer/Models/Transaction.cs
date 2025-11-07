using System;

namespace DomainAccessLayer.Models;

/// <summary>
/// Model đại diện cho giao dịch thanh toán
/// </summary>
public partial class Transaction
{
    public int TransactionId { get; set; }

    public int OrderId { get; set; }

    public string TransactionCode { get; set; } = null!;

    public decimal Amount { get; set; }

    public string PaymentMethod { get; set; } = null!;

    public string Status { get; set; } = null!; // "Pending", "Success", "Failed"

    public DateTime CreatedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public string? SessionId { get; set; }

    public string? Notes { get; set; }

    public virtual Order Order { get; set; } = null!;
}

