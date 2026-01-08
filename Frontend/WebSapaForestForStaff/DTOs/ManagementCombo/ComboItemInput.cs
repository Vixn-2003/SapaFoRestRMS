using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebSapaForestForStaff.DTOs.ManagementCombo
{
    public class ComboItemInput
    {
        [Required]
        public int MenuItemId { get; set; }

        [Required]
        [Range(1, 100, ErrorMessage = "Số lượng phải từ 1 trở lên")]
        public int Quantity { get; set; }


        // --- Thêm mấy dòng này để View không bị lỗi đỏ (DTO hiển thị) ---
        // Dấu ? nghĩa là có thể null (vì lúc gửi lên API ta không cần gửi mấy cái này)
        public string? MenuItemName { get; set; }
        public string? ImageUrl { get; set; }
        public string? CategoryName { get; set; }
        public decimal? OriginalPrice { get; set; }
    }
}
