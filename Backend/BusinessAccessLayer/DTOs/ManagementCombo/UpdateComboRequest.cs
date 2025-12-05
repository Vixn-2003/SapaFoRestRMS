using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.DTOs.ManagementCombo
{
    public class UpdateComboRequest
    {
        public string Name { get; set; }
        public decimal ActualPrice { get; set; } // Giá bán mới
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsAvailable { get; set; } // Cho phép ẩn/hiện combo
        public List<ComboItemInputDto> Items { get; set; }
    }
}
