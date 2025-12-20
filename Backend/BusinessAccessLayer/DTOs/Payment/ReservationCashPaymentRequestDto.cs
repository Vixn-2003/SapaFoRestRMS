using System.ComponentModel.DataAnnotations;

namespace BusinessAccessLayer.DTOs.Payment;

/// <summary>
/// DTO cho thanh toán tiền mặt theo ReservationId
/// </summary>
public class ReservationCashPaymentRequestDto
{
    [Required(ErrorMessage = "Số tiền khách đưa là bắt buộc")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Số tiền phải lớn hơn 0")]
    public decimal AmountReceived { get; set; }

    public string? Notes { get; set; }
}

