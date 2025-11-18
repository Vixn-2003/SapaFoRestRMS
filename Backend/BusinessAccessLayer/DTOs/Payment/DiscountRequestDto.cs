using System.ComponentModel.DataAnnotations;

namespace BusinessAccessLayer.DTOs.Payment;

/// <summary>
/// DTO cho request áp dụng ưu đãi
/// </summary>
public class DiscountRequestDto
{
    [Required(ErrorMessage = "OrderId là bắt buộc")]
    public int OrderId { get; set; }

    public string? VoucherCode { get; set; }

    public int? PromotionId { get; set; }

    public decimal? DiscountAmount { get; set; }
}

