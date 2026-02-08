using System.ComponentModel.DataAnnotations;

namespace BusinessAccessLayer.DTOs.Payment;

/// <summary>
/// DTO cho retry payment
/// </summary>
public class PaymentRetryRequestDto
{
    [Required(ErrorMessage = "TransactionId là bắt buộc")]
    public int TransactionId { get; set; }

    public string? Notes { get; set; }
}

