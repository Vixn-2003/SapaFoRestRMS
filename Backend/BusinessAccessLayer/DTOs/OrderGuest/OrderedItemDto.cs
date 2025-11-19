using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.DTOs.OrderGuest
{
    public class OrderedItemDto
    {
        public int OrderDetailId { get; set; }
        public string ItemName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice => Quantity * UnitPrice;
        public string Status { get; set; }
        // --- Thêm 2 dòng này để sửa lỗi thiếu định nghĩa ---
        public int? MenuItemId { get; set; } // Có thể null nếu là Combo
        public int? ComboId { get; set; }    // Có thể null nếu là món lẻ

        public string? Notes { get; set; }
    }
}
