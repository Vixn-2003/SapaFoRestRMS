using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.DTOs.ManagementCombo
{
    public class CreateComboDto
    {
        public string Name { get; set; }
        public decimal SellingPrice { get; set; }
        public string Description { get; set; }
        public bool IsAvailable { get; set; }
        public string? ImageUrl { get; set; }
        public IFormFile? ImageFile { get; set; }
        public List<ComboItemDto> Items { get; set; } = new();
    }
}
