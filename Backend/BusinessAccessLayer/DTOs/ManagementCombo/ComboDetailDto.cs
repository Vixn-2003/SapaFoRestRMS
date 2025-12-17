using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.DTOs.ManagementCombo
{
    public class ComboDetailDto
    {
        public int ComboId { get; set; }
        public string Name { get; set; }

        // 2 trường quan trọng cho req số 2
        public decimal OriginalPrice { get; set; } // Tổng tiền các món cộng lại
        public decimal SellingPrice { get; set; }  // Giá bán thực tế

        public string ImageUrl { get; set; }
        public decimal SavingsAmount => OriginalPrice - SellingPrice; // Tiết kiệm được bao nhiêu
        public List<MenuItemDto> Items { get; set; }
    }
}
