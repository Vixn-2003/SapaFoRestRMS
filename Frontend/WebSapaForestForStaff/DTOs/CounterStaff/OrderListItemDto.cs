using System;

namespace WebSapaForestForStaff.DTOs.CounterStaff
{
    public class OrderListItemDto
    {
        public int OrderId { get; set; }
        public string OrderCode { get; set; } = string.Empty;
        public string TableNumber { get; set; } = string.Empty;
        public DateTime? CreatedAt { get; set; }
        public decimal? TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsWaiterConfirmed { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public int? NumberOfGuests { get; set; }
    }
}

