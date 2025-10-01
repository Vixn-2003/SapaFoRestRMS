using DomainAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.DTOs
{
    public class MenuDTO
    {
        public int MenuItemId { get; set; }
        public int? CategoryId { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public string CourseType { get; set; } = null!;
        public bool? IsAvailable { get; set; }
        public string? LinkImg { get; set; }

        public MenuCategoryDTO? Category { get; set; }
    }

}
