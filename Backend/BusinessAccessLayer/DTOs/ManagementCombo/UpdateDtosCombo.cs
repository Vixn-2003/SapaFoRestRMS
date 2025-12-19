using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.DTOs.ManagementCombo
{
    public class UpdateDtosCombo
    {
        public class ComboDetailDto
        {
            public int ComboId { get; set; }
            public string Name { get; set; }
            public decimal SellingPrice { get; set; }
            public string Description { get; set; }
            public bool IsAvailable { get; set; }
            public string ImageUrl { get; set; }
            public List<ComboItemDto> Items { get; set; } = new List<ComboItemDto>();

            public decimal TotalPrice => Items?.Sum(x => x.Quantity * x.OriginalPrice) ?? 0m;

        }

        // DTO của từng món trong Combo
        public class ComboItemDto
        {
            public int MenuItemId { get; set; }
            public string MenuItemName { get; set; }
            public decimal OriginalPrice { get; set; } // Giá gốc món lẻ
            public int Quantity { get; set; }
            public string ImageUrl { get; set; }

            public string CategoryName { get; set; }

        }

        // DTO nhận dữ liệu Cập nhật từ Client gửi lên
        public class UpdateComboDto
        {
            public string Name { get; set; }
            public decimal SellingPrice { get; set; }
            public string Description { get; set; }
            public bool IsAvailable { get; set; }

            public string? ImageUrl { get; set; }
            public IFormFile? ImageFile { get; set; }
            public List<ComboItemInput> Items { get; set; }
        }

        public class ComboItemInput
        {
            public int MenuItemId { get; set; }
            public int Quantity { get; set; }
        }

        // DTO cho Menu Item (Dropdown chọn món)
        public class MenuItemDto
        {
            public int MenuItemId { get; set; }
            public string MenuItemName { get; set; }
            public decimal OriginalPrice { get; set; }
            public string ImageURL { get; set; }
            public string CategoryName { get; set; }
        }
    }
}
