using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebSapaForestForStaff.DTOs.OrderGuest
{
    public class ComboDto
    {
        public int ComboId { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public string ImageUrl { get; set; }
        public bool? IsAvailable { get; set; }
        // ⭐️ THÊM MỚI: Giá gốc (để hiển thị gạch ngang)
        public decimal? OriginalPrice { get; set; }
    }
}
