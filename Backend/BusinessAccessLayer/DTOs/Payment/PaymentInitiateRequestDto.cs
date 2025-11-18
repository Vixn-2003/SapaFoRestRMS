using System.ComponentModel.DataAnnotations;

namespace BusinessAccessLayer.DTOs.Payment;

/// <summary>
/// DTO cho request khởi tạo thanh toán
/// </summary>
public class PaymentInitiateRequestDto
{
    [Required(ErrorMessage = "OrderId là bắt buộc")]
    public int OrderId { get; set; }

    [Required(ErrorMessage = "PaymentMethod là bắt buộc")]
    public string PaymentMethod { get; set; } = null!;

    [Required(ErrorMessage = "Amount là bắt buộc")]
    public decimal Amount { get; set; }
}

