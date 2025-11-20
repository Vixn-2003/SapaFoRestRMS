using System.Collections.Generic;
using WebSapaForestForStaff.DTOs.Kitchen;

namespace WebSapaForestForStaff.Models.Kitchen
{
    /// <summary>
    /// ViewModel for Kitchen Display Index page (Sous Chef screen)
    /// </summary>
    public class KitchenDisplayViewModel
    {
        public List<KitchenOrderCardDto> ActiveOrders { get; set; } = new();
        public List<string> CourseTypes { get; set; } = new();
        public string ApiBaseUrl { get; set; } = string.Empty;
        public string SignalRHubUrl { get; set; } = string.Empty;
    }
}

