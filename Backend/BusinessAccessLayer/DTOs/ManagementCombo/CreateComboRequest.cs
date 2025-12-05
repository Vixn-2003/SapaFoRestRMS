using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.DTOs.ManagementCombo
{
    public class CreateComboRequest
    {
        [Required(ErrorMessage = "Tên combo không được để trống")]
        public string Name { get; set; }

        public string? Description { get; set; }

        public string? ImageUrl { get; set; }

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Giá bán phải lớn hơn 0")]
        public decimal ActualPrice { get; set; } // Giá bán thực tế (nhập tay)

        [Required]
        [MinLength(1, ErrorMessage = "Combo phải có ít nhất 1 món")]
        public List<ComboItemInputDto> Items { get; set; }
    }
}
