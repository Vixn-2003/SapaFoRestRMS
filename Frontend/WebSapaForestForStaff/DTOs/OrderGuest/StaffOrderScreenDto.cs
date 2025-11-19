using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebSapaForestForStaff.DTOs.OrderGuest
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

        // --- Thông tin Menu (Bên trái) ---
        public List<MenuItemDto> MenuItems { get; set; } = new List<MenuItemDto>();
        public List<ComboDto> Combos { get; set; } = new List<ComboDto>();
    }
}
