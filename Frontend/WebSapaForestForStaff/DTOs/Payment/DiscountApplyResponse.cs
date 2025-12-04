namespace WebSapaForestForStaff.DTOs.Payment
{
    /// <summary>
    /// Kết quả từ API áp dụng ưu đãi /payment/discounts/validate
    /// </summary>
    public class DiscountApplyResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public OrderDetailDto? Order { get; set; }
    }
}


