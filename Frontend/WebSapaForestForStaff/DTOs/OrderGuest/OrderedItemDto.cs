using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebSapaForestForStaff.DTOs.OrderGuest
{
    public class OrderedItemDto
    {
        public int OrderDetailId { get; set; }
        public string ItemName { get; set; }

    public int? MenuItemId { get; set; }
    public int? ComboId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice => Quantity * UnitPrice;
        public string Status { get; set; }

        public string? Notes { get; set; }

        [NotMapped] // Nếu không muốn lưu vào DB
        public bool IsCustomerOrder { get; set; }
    }
}
