using System.ComponentModel.DataAnnotations;

namespace BusinessAccessLayer.DTOs.Payment;

/// <summary>
/// DTO cho request xác nhận thanh toán
/// POST /api/payments/confirm
/// </summary>
public class PaymentConfirmRequestDto
{
    [Required(ErrorMessage = "OrderId là bắt buộc")]
    public int OrderId { get; set; }

    [Required(ErrorMessage = "TransactionId là bắt buộc")]
    public int TransactionId { get; set; }

    /// <summary>
    /// Số tiền khách đưa (cho Cash payment)
    /// </summary>
    public decimal? CashGiven { get; set; }

    /// <summary>
    /// Tiền thối lại (cho Cash payment)
    /// </summary>
    public decimal? Change { get; set; }

    /// <summary>
    /// Mã tham chiếu từ gateway (cho QR/Card/EWallet)
    /// </summary>
    public string? GatewayReference { get; set; }

    /// <summary>
    /// Ghi chú
    /// </summary>
    public string? Notes { get; set; }
}

