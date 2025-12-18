using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebSapaForestForStaff.DTOs.ManagementCombo
{
  
    public class CreateComboDto
    {
        [Required]
        public string Name { get; set; }
        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Giá bán phải lớn hơn 0")]
        public decimal SellingPrice { get; set; }
        public string Description { get; set; }
        public bool IsAvailable { get; set; }
        public string ImageUrl { get; set; }
        [Required]
        [MinLength(1, ErrorMessage = "Combo phải có ít nhất 1 món")]
        public List<ComboItemInput> Items { get; set; } = new();
    }
}
