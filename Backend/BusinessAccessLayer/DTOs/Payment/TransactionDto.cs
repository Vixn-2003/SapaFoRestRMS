using System;

namespace BusinessAccessLayer.DTOs.Payment;

/// <summary>
/// DTO cho thông tin giao dịch thanh toán
/// </summary>
public class TransactionDto
{
    public int TransactionId { get; set; }

    public int OrderId { get; set; }

    public string TransactionCode { get; set; } = null!;

    public decimal Amount { get; set; }

    public string PaymentMethod { get; set; } = null!;

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public string? SessionId { get; set; }

    public string? Notes { get; set; }
}

