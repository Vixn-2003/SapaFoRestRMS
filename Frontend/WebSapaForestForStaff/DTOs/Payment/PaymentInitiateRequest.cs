namespace WebSapaForestForStaff.DTOs.Payment
{
    /// <summary>
    /// Request DTO cho việc khởi tạo thanh toán
    /// </summary>
    public class PaymentInitiateRequest
    {
        public int OrderId { get; set; }
        public string Method { get; set; } = "cash"; // cash, qr, card, ewallet
        public decimal? Amount { get; set; }
    }
}

