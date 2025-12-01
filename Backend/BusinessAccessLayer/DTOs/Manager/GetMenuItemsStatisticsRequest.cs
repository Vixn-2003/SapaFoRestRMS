using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.DTOs.Manager
{
    public class GetMenuItemsStatisticsRequest
    {
        public int RestaurantId { get; set; }
        public int? CategoryId { get; set; }
        public string CourseType { get; set; }
        public DateTime? Date { get; set; }
        public string SearchKeyword { get; set; }
        public bool? IsAvailableOnly { get; set; }
        public string SortBy { get; set; } // "name", "servedToday", "trend"
        public string SortOrder { get; set; } // "asc", "desc"
    }
}
