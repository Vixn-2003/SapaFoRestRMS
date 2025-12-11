using System.ComponentModel.DataAnnotations;

namespace BusinessAccessLayer.DTOs.Payment;

/// <summary>
/// DTO cho request thanh toán kết hợp (Cash + QR)
/// </summary>
public class CombinedPaymentRequestDto
{
    [Required(ErrorMessage = "OrderId là bắt buộc")]
    public int OrderId { get; set; }

    [Required(ErrorMessage = "Số tiền thanh toán bằng tiền mặt là bắt buộc")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Số tiền phải lớn hơn 0")]
    public decimal CashAmount { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Số tiền khách đưa không được âm")]
    public decimal? CashReceived { get; set; }

    [Required(ErrorMessage = "Số tiền thanh toán bằng QR là bắt buộc")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Số tiền phải lớn hơn 0")]
    public decimal QrAmount { get; set; }

    public string? Notes { get; set; }
}

