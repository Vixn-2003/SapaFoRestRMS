using System.ComponentModel.DataAnnotations;

namespace BusinessAccessLayer.DTOs.Payment;

/// <summary>
/// DTO cho request bắt đầu thanh toán
/// GET /api/payments/start/{orderId}
/// </summary>
public class PaymentStartRequestDto
{
    [Required(ErrorMessage = "OrderId là bắt buộc")]
    public int OrderId { get; set; }

    [Required(ErrorMessage = "PaymentMethod là bắt buộc")]
    public string PaymentMethod { get; set; } = null!; // "Cash", "QRBank", "Card", "EWallet"
}

