using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebSapaForestForStaff.DTOs.ManagementCombo
{
    public class MenuItemDto
    {
        public int MenuItemId { get; set; }
        public string MenuItemName { get; set; } = string.Empty;
        public decimal OriginalPrice { get; set; }
        public string? ImageUrl { get; set; }
        public string? CategoryName { get; set; } // Map từ MenuCategories
        public bool IsAvailable { get; set; }
        public string? Description { get; set; }
        public int Quantity { get; set; }

        public List<ComboDisplayDto> Combos { get; set; }
    }
}
