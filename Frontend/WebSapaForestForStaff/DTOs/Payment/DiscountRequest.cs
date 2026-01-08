namespace WebSapaForestForStaff.DTOs.Payment
{
    /// <summary>
    /// Request áp dụng ưu đãi/giảm giá cho đơn hàng ở frontend
    /// Mapping với DiscountRequestDto bên API
    /// </summary>
    public class DiscountRequest
    {
        public int OrderId { get; set; }
        public string? VoucherCode { get; set; }
        public int? PromotionId { get; set; }
        public decimal? DiscountAmount { get; set; }
    }
}


