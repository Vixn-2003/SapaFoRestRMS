using System.Collections.Generic;

namespace WebSapaForestForStaff.DTOs.Payment
{
    public class OrderListResponseDto
    {
        public List<OrderDto> Orders { get; set; } = new();
        public int TotalOrders { get; set; }
        public int PendingOrders { get; set; }
        public int ProcessedOrders { get; set; }
        public string? SelectedDate { get; set; }
    }
}

