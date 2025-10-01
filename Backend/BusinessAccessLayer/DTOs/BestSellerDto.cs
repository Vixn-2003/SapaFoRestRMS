using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.DTOs
{
    public class BestSellerDto
    {
        public int MenuItemId { get; set; }
        public string MenuItemName { get; set; } = null!;
        public int TotalQuantity { get; set; }

        public string? Description { get; set; }   // Mô tả món
        public string? ImageUrl { get; set; }      // Ảnh món
    }
}
