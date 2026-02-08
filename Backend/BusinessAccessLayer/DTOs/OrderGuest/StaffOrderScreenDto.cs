using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static BusinessAccessLayer.Services.OrderTableService;

namespace BusinessAccessLayer.DTOs.OrderGuest
{
    public class StaffOrderScreenDto
    {
        // --- Thông tin Order (Bên phải) ---
        public int TableId { get; set; }
        public string TableNumber { get; set; }
        public string AreaName { get; set; }
        public int Floor { get; set; }

        public int? ReservationId { get; set; }
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public int GuestCount { get; set; }
        public List<OrderedItemDto> OrderedItems { get; set; } = new List<OrderedItemDto>();
        public decimal GrandTotal => OrderedItems.Sum(item => item.TotalPrice);
        public int? ActiveOrderId { get; set; }
        public string? OrderStatus { get; set; } // Trạng thái order: "waiting-confirmation", "confirmed", etc.

        // --- Thống kê số lượng món theo trạng thái ---
        public int TotalQuantity { get; set; } // Tổng số lượng món đã gọi
        public int QtyServedAndCooking { get; set; } // Đã phục vụ & đang nấu (Status = Cooking, Done, Ready, Served)
        public int QtyNotCooked { get; set; } // Chưa nấu (Status = Pending)
        public int QtyCancelled { get; set; } // Món đã hủy (Status = Cancelled, Removed)

        // --- Thông tin Menu (Bên trái) ---
        public List<MenuItemDto> MenuItems { get; set; } = new List<MenuItemDto>();
        public List<ComboDto> Combos { get; set; } = new List<ComboDto>();
    }
}
