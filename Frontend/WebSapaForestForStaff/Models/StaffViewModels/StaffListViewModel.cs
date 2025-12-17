using WebSapaForestForStaff.DTOs.Staff;

namespace WebSapaForestForStaff.Models.StaffViewModels
{
    /// <summary>
    /// ViewModel for Staff List page
    /// UC55 - View List Staff
    /// </summary>
    public class StaffListViewModel
    {
        public List<StaffListItemDto> StaffList { get; set; } = new();
        public StaffFilterDto Filter { get; set; } = new();
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
    }
}

