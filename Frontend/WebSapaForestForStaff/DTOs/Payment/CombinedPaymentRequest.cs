namespace WebSapaForestForStaff.DTOs.Payment
{
    /// <summary>
    /// Request thanh toán kết hợp (Cash + QR)
    /// </summary>
    public class CombinedPaymentRequest
    {
        public int OrderId { get; set; }
        public decimal CashAmount { get; set; }
        public decimal? CashReceived { get; set; }
        public decimal QrAmount { get; set; }
        public string? Notes { get; set; }
    }
}

