namespace WebSapaForestForStaff.DTOs.Payment
{
    /// <summary>
    /// Request DTO cho việc khởi tạo thanh toán (map 1-1 với PaymentInitiateRequestDto bên Backend)
    /// </summary>
    public class PaymentInitiateRequest
    {
        public int OrderId { get; set; }
        /// <summary>
        /// Tên phương thức thanh toán, phải khớp với PaymentMethod bên API (cash, qr, card, ewallet,...)
        /// </summary>
        public string PaymentMethod { get; set; } = "cash";
        public decimal? Amount { get; set; }
    }
}

