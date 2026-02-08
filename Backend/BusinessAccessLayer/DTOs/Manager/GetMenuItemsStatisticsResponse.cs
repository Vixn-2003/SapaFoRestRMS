using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.DTOs.Manager
{
    public class GetMenuItemsStatisticsResponse
    {
        public List<MenuItemStatisticsDto> Data { get; set; }
        public int TotalItems { get; set; }
        public DateTime GeneratedAt { get; set; }
    }
}
