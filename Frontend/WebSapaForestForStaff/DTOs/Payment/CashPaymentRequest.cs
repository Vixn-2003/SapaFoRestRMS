using System.ComponentModel.DataAnnotations;

namespace WebSapaForestForStaff.DTOs.Payment;

/// <summary>
/// Request DTO cho thanh toán tiền mặt
/// </summary>
public class CashPaymentRequest
{
    [Required(ErrorMessage = "OrderId là bắt buộc")]
    public int OrderId { get; set; }

    [Required(ErrorMessage = "Số tiền khách đưa là bắt buộc")]
    [Range(0, double.MaxValue, ErrorMessage = "Số tiền phải lớn hơn hoặc bằng 0")]
    public decimal AmountReceived { get; set; }

    public string? Notes { get; set; }
}

