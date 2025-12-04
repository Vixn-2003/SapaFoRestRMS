using DomainAccessLayer.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.DTOs.Manager
{
    public class MenuItemStatisticsDto
    {
        public int MenuItemId { get; set; }
        public int? CategoryId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public string CourseType { get; set; }
        public string ImageUrl { get; set; }
        public bool IsAvailable { get; set; }
        public bool IsAds { get; set; }
        public int? TimeCook { get; set; }
        public ItemBillingType BillingType { get; set; }

        public List<RecipeDTO> Recipes { get; set; }

        // Số lượng đã bán
        public int ServedToday { get; set; }
        public int ServedYesterday { get; set; }
        public double Average7Days { get; set; }
        public double Average30Days { get; set; }
        public double Average90Days { get; set; }

        // % so sánh
        public double CompareWithYesterday { get; set; }
        public double CompareWith7Days { get; set; }
        public double CompareWith30Days { get; set; }
    }
}
