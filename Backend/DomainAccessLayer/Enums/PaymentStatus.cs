namespace DomainAccessLayer.Enums;

/// <summary>
/// Trạng thái thanh toán
/// Simplified: Only essential states for Cash and QR payments
/// </summary>
public enum PaymentStatus
{
    /// <summary>
    /// Chờ thanh toán (Pending)
    /// </summary>
    WaitingForPayment = 1,

    /// <summary>
    /// Đã thanh toán (Paid)
    /// </summary>
    Paid = 3,

    /// <summary>
    /// Đã hủy (Cancelled)
    /// </summary>
    Cancelled = 4

    // Removed: PaymentProcessing, Failed, PartiallyPaid - Simplified for Cash/QR only
    // Note: PartiallyPaid can be handled by multiple transactions with status "Paid"
}

