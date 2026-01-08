using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebSapaForestForStaff.DTOs.ManagementCombo
{
    public class ComboEditDto
    {
        // ====== COMBO INFO ======
        public int ComboId { get; set; }
        [Required(ErrorMessage = "Tên combo không được để trống")]
        public string Name { get; set; }
        [Required(ErrorMessage = "Tên mô tả không được để trống")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập giá bán")]
        [Range(1, double.MaxValue, ErrorMessage = "Giá bán phải lớn hơn 0")]
        public decimal SellingPrice { get; set; }
        public bool IsAvailable { get; set; }
        [Required]
        public string? ImageUrl { get; set; }
        public IFormFile? ImageFile { get; set; }

        // ====== ITEMS ======
        public List<ComboItemUpdateDTO> Items { get; set; } = new();

        // ====== CALCULATED (FOR VIEW) ======
        public decimal TotalPrice
            => Items?.Sum(i => i.OriginalPrice * i.Quantity) ?? 0m;

        public decimal SavingsAmount
            => TotalPrice - SellingPrice;
    }
}
