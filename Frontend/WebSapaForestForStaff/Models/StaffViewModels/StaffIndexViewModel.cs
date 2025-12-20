using WebSapaForestForStaff.DTOs.Staff;
using WebSapaForestForStaff.Services.Api.Interfaces;

namespace WebSapaForestForStaff.Models.StaffViewModels
{
    /// <summary>
    /// ViewModel for Staff Index page
    /// </summary>
    public class StaffIndexViewModel
    {
        /// <summary>
        /// Filter criteria
        /// </summary>
        public StaffFilterDto Filter { get; set; } = new StaffFilterDto();

        /// <summary>
        /// Staff list data
        /// </summary>
        public StaffListResponse? StaffList { get; set; }

        /// <summary>
        /// Available positions for filter dropdown
        /// </summary>
        public List<PositionDto> AvailablePositions { get; set; } = new();
    }
}

