using System;

namespace BusinessAccessLayer.DTOs.ReservationDepositDto
{
    public class ReservationDepositResponseDto
    {
        public int DepositId { get; set; }
        public int ReservationId { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = null!;
        public string DepositCode { get; set; } = null!;
        public DateTime DepositDate { get; set; }
        public string? Notes { get; set; }
        public string? ReceiptImageUrl { get; set; }
    }
}
