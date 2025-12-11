using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.DTOs.ManagementCombo
{
    public class TopSellerDto
    {
        public int Id { get; set; } // MenuItemId hoặc ComboId
        public string Name { get; set; }
        public string? ImageUrl { get; set; }
        public int TotalSold { get; set; } // Số lượng đã bán (Status = "Completed")
        public string Type { get; set; } // "SingleItem" hoặc "Combo" (Optional)
    }
}
