using BusinessAccessLayer.Common.Pagination;
using BusinessAccessLayer.DTOs.Positions;
using BusinessAccessLayer.DTOs.Staff;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BusinessAccessLayer.Services.Interfaces
{
    /// <summary>
    /// Service interface for Staff Management business logic
    /// UC55 - View List Staff
    /// UC56 - Update Staff
    /// UC57 - Deactivate / Delete Staff
    /// Create Staff
    /// </summary>
    public interface IStaffManagementService
    {
        /// <summary>
        /// UC55 - Get paginated staff list with filters
        /// </summary>
        Task<PagedResult<StaffListItemDto>> GetStaffListAsync(
            StaffFilterDto filter,
            int? managerDepartmentId = null,
            CancellationToken ct = default);

        /// <summary>
        /// Get staff detail by ID
        /// </summary>
        Task<StaffDetailDto?> GetStaffDetailAsync(int staffId, CancellationToken ct = default);

        /// <summary>
        /// Create new staff
        /// </summary>
        Task<(bool Success, int? StaffId, string Message)> CreateStaffAsync(
            StaffCreateDto dto,
            int createdBy,
            string? ipAddress = null,
            CancellationToken ct = default);

        /// <summary>
        /// UC56 - Update existing staff
        /// </summary>
        Task<(bool Success, string Message)> UpdateStaffAsync(
            StaffUpdateDto dto,
            int modifiedBy,
            string? ipAddress = null,
            CancellationToken ct = default);

        /// <summary>
        /// UC57 - Deactivate staff (soft delete)
        /// </summary>
        Task<(bool Success, string Message)> DeactivateStaffAsync(
            StaffDeactivateDto dto,
            int deletedBy,
            string? ipAddress = null,
            CancellationToken ct = default);

        /// <summary>
        /// Change staff status (0 = Active, 1 = Inactive)
        /// </summary>
        Task<(bool Success, string Message)> ChangeStatusAsync(
            int staffId,
            int status,
            int modifiedBy,
            string? ipAddress = null,
            CancellationToken ct = default);

        /// <summary>
        /// Get available positions for dropdown
        /// </summary>
        Task<List<PositionDto>> GetActivePositionsAsync(CancellationToken ct = default);

        /// <summary>
        /// Validate if manager can manage this staff
        /// </summary>
        Task<bool> CanManagerManageStaffAsync(int managerId, int staffId, CancellationToken ct = default);
    }
}

