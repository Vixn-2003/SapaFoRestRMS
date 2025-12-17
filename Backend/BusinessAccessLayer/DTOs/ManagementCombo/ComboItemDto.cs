using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.DTOs.ManagementCombo
{
    public class ComboItemDto
    {
        public int MenuItemId { get; set; }
        public string Name { get; set; }
        public string? ImageUrl { get; set; }
        public decimal OriginalPrice { get; set; }
        public int Quantity { get; set; }
    }
}
