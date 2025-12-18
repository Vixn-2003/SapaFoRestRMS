using WebSapaForestForStaff.DTOs.Staff;

namespace WebSapaForestForStaff.Services.Api.Interfaces
{
    /// <summary>
    /// Interface for Staff Management API Service
    /// UC55 - View List Staff
    /// UC56 - Update Staff
    /// UC57 - Deactivate / Delete Staff
    /// Create Staff
    /// </summary>
    public interface IStaffManagementApiService : IBaseApiService
    {
        /// <summary>
        /// UC55 - Get paginated list of staff with filters
        /// </summary>
        Task<(bool Success, StaffListResponse? Data, string? Message)> GetStaffListAsync(StaffFilterDto filter);

        /// <summary>
        /// Get staff detail by ID
        /// </summary>
        Task<(bool Success, StaffDetailDto? Data, string? Message)> GetStaffDetailAsync(int staffId);

        /// <summary>
        /// Create new staff
        /// </summary>
        Task<(bool Success, int? StaffId, string? Message)> CreateStaffAsync(StaffCreateDto dto);

        /// <summary>
        /// UC56 - Update existing staff
        /// </summary>
        Task<(bool Success, string? Message)> UpdateStaffAsync(int staffId, StaffUpdateDto dto);

        /// <summary>
        /// UC57 - Deactivate staff
        /// </summary>
        Task<(bool Success, string? Message)> DeactivateStaffAsync(int staffId, StaffDeactivateDto dto);

        /// <summary>
        /// Get active positions for dropdown
        /// </summary>
        Task<(bool Success, List<PositionDto>? Data, string? Message)> GetActivePositionsAsync();

        /// <summary>
        /// Change staff status (Activate/Deactivate)
        /// </summary>
        Task<(bool Success, string? Message)> ChangeStaffStatusAsync(int staffId, int status);

        /// <summary>
        /// Reset staff password
        /// </summary>
        Task<(bool Success, string? Message)> ResetStaffPasswordAsync(int staffId);
    }

    /// <summary>
    /// Response model for staff list
    /// </summary>
    public class StaffListResponse
    {
        public List<StaffListItemDto> Data { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
    }
}

