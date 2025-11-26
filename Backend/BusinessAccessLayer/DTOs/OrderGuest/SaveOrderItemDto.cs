    using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.DTOs.OrderGuest
{
    public class SaveOrderItemDto
    {
        public int OrderItemId { get; set; } // 0 nếu là món mới

        public int? MenuItemId { get; set; } // Nullable vì có thể là Combo
        public int? ComboId { get; set; }    // Nullable vì có thể là Món lẻ

        public int Quantity { get; set; }

        public string? Note { get; set; }    // Ghi chú (có thể null)

        // Quan trọng: Cờ để phân loại hành động (Add, Update, Delete)
        public string Action { get; set; } = string.Empty;


    }
}
