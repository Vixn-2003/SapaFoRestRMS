using System.ComponentModel.DataAnnotations;

namespace BusinessAccessLayer.DTOs.Payment;

/// <summary>
/// DTO cho request thanh toán
/// </summary>
public class PaymentRequestDto
{
    [Required(ErrorMessage = "OrderId là bắt buộc")]
    public int OrderId { get; set; }

    [Required(ErrorMessage = "PaymentMethod là bắt buộc")]
    public string PaymentMethod { get; set; } = null!; // "Cash", "Card", "EWallet", "Split"

    [Required(ErrorMessage = "Amount là bắt buộc")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount phải lớn hơn 0")]
    public decimal Amount { get; set; }

    public decimal? CashGiven { get; set; } // Cho thanh toán tiền mặt

    public decimal? Change { get; set; } // Tiền thối lại

    public int? SplitCount { get; set; } // Số người chia bill

    public string? SessionId { get; set; } // Cho thanh toán số (Card, EWallet)

    public string? Notes { get; set; }
}

