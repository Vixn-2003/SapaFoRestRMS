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

    /// <summary>
    /// Số tiền khách đưa (cho Cash payment)
    /// </summary>
    public decimal? AmountReceived { get; set; }

    /// <summary>
    /// Tiền thối lại (cho Cash payment)
    /// </summary>
    public decimal? RefundAmount { get; set; }

    public string PaymentMethod { get; set; } = null!;

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public string? SessionId { get; set; }

    /// <summary>
    /// Mã tham chiếu từ gateway (transaction ID từ VNPay, MoMo, etc.)
    /// </summary>
    public string? GatewayReference { get; set; }

    /// <summary>
    /// Mã lỗi từ gateway
    /// </summary>
    public string? GatewayErrorCode { get; set; }

    /// <summary>
    /// Thông báo lỗi từ gateway
    /// </summary>
    public string? GatewayErrorMessage { get; set; }

    /// <summary>
    /// Số lần retry
    /// </summary>
    public int RetryCount { get; set; }

    /// <summary>
    /// Đã được xác nhận thủ công
    /// </summary>
    public bool IsManualConfirmed { get; set; }

    /// <summary>
    /// ID transaction cha (cho Split Bill)
    /// </summary>
    public int? ParentTransactionId { get; set; }

    public string? Notes { get; set; }
}

