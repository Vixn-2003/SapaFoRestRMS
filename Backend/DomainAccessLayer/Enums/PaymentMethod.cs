namespace DomainAccessLayer.Enums;

/// <summary>
/// Phương thức thanh toán
/// Simplified: Only Cash and QR (VietQR) payments supported
/// </summary>
public enum PaymentMethod
{
    /// <summary>
    /// Tiền mặt
    /// </summary>
    Cash = 1,

    /// <summary>
    /// Chuyển khoản qua QR (VietQR) - Manual confirmation
    /// </summary>
    QRBankTransfer = 2

    // Removed: Card, EWallet, DiscountCoupon - Simplified payment system
}

