using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.DTOs.ManagementCombo
{
    public class MenuItemDto
    {
        public int MenuItemId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string? ImageUrl { get; set; }
        public string? CategoryName { get; set; } // Map từ MenuCategories
        public bool IsAvailable { get; set; }
        public string? Description { get; set; }
        public int Quantity { get; set; }
        public List<ComboDisplayDto> Combos { get; set; }
    }
}
