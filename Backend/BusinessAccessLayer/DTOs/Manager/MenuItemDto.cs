using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.DTOs.Manager
{
    public class MenuItemDto
    {
        public int MenuItemId { get; set; }
        public int? CategoryId { get; set; }
        public string CategoryName { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public string CourseType { get; set; }
        public bool IsAvailable { get; set; }
        public string ImageUrl { get; set; }
        public int? TimeCook { get; set; }
        public int? BatchSize { get; set; }
    }
}
