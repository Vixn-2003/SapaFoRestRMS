using System;
using DomainAccessLayer.Enums;

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

    /// <summary>
    /// Số tiền khách đưa (cho Cash payment)
    /// </summary>
    public decimal? AmountReceived { get; set; }

    /// <summary>
    /// Tiền thối lại (cho Cash payment)
    /// </summary>
    public decimal? RefundAmount { get; set; }

    public string PaymentMethod { get; set; } = null!;

    public string Status { get; set; } = null!; // "WaitingForPayment", "PaymentProcessing", "Paid", "Failed", "Cancelled", "PartiallyPaid"

    public DateTime CreatedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public string? SessionId { get; set; }

    /// <summary>
    /// Mã tham chiếu từ gateway (transaction ID từ VNPay, MoMo, etc.)
    /// </summary>
    public string? GatewayReference { get; set; }

    /// <summary>
    /// Mã lỗi từ gateway (nếu có)
    /// </summary>
    public string? GatewayErrorCode { get; set; }

    /// <summary>
    /// Thông báo lỗi từ gateway
    /// </summary>
    public string? GatewayErrorMessage { get; set; }

    /// <summary>
    /// Số lần retry
    /// </summary>
    public int RetryCount { get; set; } = 0;

    /// <summary>
    /// Thời gian retry cuối cùng
    /// </summary>
    public DateTime? LastRetryAt { get; set; }

    public string? Notes { get; set; }

    /// <summary>
    /// ID của transaction cha (cho split bill)
    /// </summary>
    public int? ParentTransactionId { get; set; }

    /// <summary>
    /// Đã được xác nhận thủ công bởi staff
    /// </summary>
    public bool IsManualConfirmed { get; set; } = false;

    /// <summary>
    /// User ID xác nhận thủ công
    /// </summary>
    public int? ConfirmedByUserId { get; set; }

    public virtual Order Order { get; set; } = null!;

    public virtual Transaction? ParentTransaction { get; set; }

    public virtual ICollection<Transaction> ChildTransactions { get; set; } = new List<Transaction>();

    public virtual User? ConfirmedByUser { get; set; }
}

