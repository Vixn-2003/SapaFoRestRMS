using DomainAccessLayer.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories.Interfaces
{
    /// <summary>
    /// Repository interface for Staff Management operations
    /// UC55 - View List Staff
    /// UC56 - Update Staff
    /// UC57 - Deactivate / Delete Staff
    /// Create Staff
    /// </summary>
    public interface IStaffManagementRepository
    {
        /// <summary>
        /// Get staff query for filtering (returns IQueryable for service layer processing)
        /// </summary>
        Task<(IQueryable<Staff> Query, int TotalCount)> GetStaffQueryAsync(
            int? departmentId,
            string? searchKeyword,
            string? position,
            int? status,
            string sortBy,
            string sortDirection,
            CancellationToken ct = default);

        /// <summary>
        /// Get staff by ID with all related data
        /// </summary>
        Task<Staff?> GetStaffByIdAsync(int staffId, CancellationToken ct = default);

        /// <summary>
        /// Get staff by User ID
        /// </summary>
        Task<Staff?> GetStaffByUserIdAsync(int userId, CancellationToken ct = default);

        /// <summary>
        /// Check if email already exists
        /// </summary>
        Task<bool> EmailExistsAsync(string email, int? excludeUserId = null, CancellationToken ct = default);

        /// <summary>
        /// Create new staff
        /// </summary>
        Task<Staff> CreateStaffAsync(Staff staff, CancellationToken ct = default);

        /// <summary>
        /// Update existing staff
        /// </summary>
        Task<bool> UpdateStaffAsync(Staff staff, CancellationToken ct = default);

        /// <summary>
        /// Deactivate staff (soft delete)
        /// </summary>
        Task<bool> DeactivateStaffAsync(int staffId, string? reason, CancellationToken ct = default);

        /// <summary>
        /// Change staff status (0 = Active, 1 = Inactive)
        /// </summary>
        Task<bool> ChangeStaffStatusAsync(int staffId, int status, CancellationToken ct = default);

        /// <summary>
        /// Check if staff exists and is not deleted
        /// </summary>
        Task<bool> StaffExistsAsync(int staffId, CancellationToken ct = default);

        /// <summary>
        /// Get all active positions
        /// </summary>
        Task<List<Position>> GetActivePositionsAsync(CancellationToken ct = default);

        /// <summary>
        /// Add position to staff (insert into StaffPosition junction table)
        /// Enforces 1 position per staff rule
        /// </summary>
        Task AddStaffPositionAsync(int staffId, int positionId, CancellationToken ct = default);

        /// <summary>
        /// Get staff count in department
        /// </summary>
        Task<int> GetStaffCountInDepartmentAsync(int departmentId, CancellationToken ct = default);
    }
}

