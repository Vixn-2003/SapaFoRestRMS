using WebSapaForestForStaff.DTOs.Kitchen;

namespace WebSapaForestForStaff.Models.Kitchen
{
    /// <summary>
    /// ViewModel for Kitchen Station page
    /// </summary>
    public class KitchenStationViewModel
    {
        public string CategoryName { get; set; } = string.Empty;
        public StationItemsResponse? StationItems { get; set; }
        public string ApiBaseUrl { get; set; } = string.Empty;
        public string SignalRHubUrl { get; set; } = string.Empty;
    }
}

