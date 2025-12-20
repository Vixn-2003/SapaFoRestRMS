namespace WebSapaForestForStaff.DTOs.Payment
{
    /// <summary>
    /// Request áp dụng ưu đãi/giảm giá cho Reservation ở frontend
    /// Mapping với ReservationDiscountRequestDto bên API
    /// </summary>
    public class ReservationDiscountRequest
    {
        public int ReservationId { get; set; }
        public string? VoucherCode { get; set; }
        public int? PromotionId { get; set; }
        public decimal? DiscountAmount { get; set; }
    }
}

