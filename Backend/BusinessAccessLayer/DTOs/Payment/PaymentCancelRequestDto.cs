using System.ComponentModel.DataAnnotations;

namespace BusinessAccessLayer.DTOs.Payment;

/// <summary>
/// DTO cho request hủy thanh toán
/// POST /api/payments/cancel
/// </summary>
public class PaymentCancelRequestDto
{
    [Required(ErrorMessage = "OrderId là bắt buộc")]
    public int OrderId { get; set; }

    [Required(ErrorMessage = "TransactionId là bắt buộc")]
    public int TransactionId { get; set; }

    /// <summary>
    /// Lý do hủy
    /// </summary>
    public string? Reason { get; set; }
}

