using System;
using System.Collections.Generic;

namespace BusinessAccessLayer.DTOs.Payment
{
    public class OrderListResponseDto
    {
        public DateOnly SelectedDate { get; set; }
        public int TotalOrders { get; set; }
        public int PendingOrders { get; set; }
        public int ProcessedOrders { get; set; }
        public IEnumerable<OrderDto> Orders { get; set; } = new List<OrderDto>();
    }
}


