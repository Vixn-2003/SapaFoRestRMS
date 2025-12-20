using System.ComponentModel.DataAnnotations;

namespace BusinessAccessLayer.DTOs.Payment;

/// <summary>
/// DTO cho request áp dụng ưu đãi theo Reservation
/// </summary>
public class ReservationDiscountRequestDto
{
    [Required(ErrorMessage = "ReservationId là bắt buộc")]
    public int ReservationId { get; set; }

    public string? VoucherCode { get; set; }

    public int? PromotionId { get; set; }

    public decimal? DiscountAmount { get; set; }
}

