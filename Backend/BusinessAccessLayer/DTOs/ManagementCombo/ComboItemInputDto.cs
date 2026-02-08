using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.DTOs.ManagementCombo
{
    public class ComboItemInputDto
    {
        [Required]
        public int MenuItemId { get; set; }

        [Required]
        [Range(1, 100, ErrorMessage = "Số lượng phải từ 1 trở lên")]
        public int Quantity { get; set; }

        public decimal Price { get; set; }
        public string Name { get; set; }
    }
}
