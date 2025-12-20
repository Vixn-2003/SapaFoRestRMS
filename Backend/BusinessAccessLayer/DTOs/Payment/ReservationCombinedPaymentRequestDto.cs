using System.ComponentModel.DataAnnotations;

namespace BusinessAccessLayer.DTOs.Payment;

/// <summary>
/// DTO cho thanh toán kết hợp (Cash + QR) theo ReservationId
/// </summary>
public class ReservationCombinedPaymentRequestDto
{
    [Required(ErrorMessage = "Số tiền tiền mặt là bắt buộc")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Số tiền tiền mặt phải lớn hơn 0")]
    public decimal CashAmount { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Số tiền QR phải lớn hơn 0")]
    public decimal QrAmount { get; set; }

    /// <summary>
    /// Số tiền khách đưa cho phần tiền mặt (có thể lớn hơn CashAmount để tính tiền thối)
    /// </summary>
    public decimal? CashReceived { get; set; }

    public string? Notes { get; set; }
}

