using DataAccessLayer.Dbcontext;
using DataAccessLayer.Repositories.Interfaces;
using DomainAccessLayer.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories
{
    /// <summary>
    /// Repository implementation for Staff Management - works only with Domain Models
    /// </summary>
    public class StaffManagementRepository : IStaffManagementRepository
    {
        private readonly SapaFoRestRmsContext _context;

        public StaffManagementRepository(SapaFoRestRmsContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Get staff query for filtering
        /// </summary>
        public async Task<(IQueryable<Staff> Query, int TotalCount)> GetStaffQueryAsync(
            int? departmentId,
            string? searchKeyword,
            string? position,
            int? status,
            string sortBy,
            string sortDirection,
            CancellationToken ct = default)
        {
            var query = _context.Staffs
                .Include(s => s.User)
                    .ThenInclude(u => u.Role)
                .Include(s => s.Department)
                .Include(s => s.Positions)
                .Where(s => s.User != null && s.User.IsDeleted != true)
                .AsQueryable();

            // Filter by department
            if (departmentId.HasValue)
            {
                query = query.Where(s => s.DepartmentId == departmentId.Value);
            }

            // Filter by search keyword (name, phone, email)
            if (!string.IsNullOrWhiteSpace(searchKeyword))
            {
                var keyword = searchKeyword.Trim().ToLower();
                query = query.Where(s =>
                    s.User.FullName.ToLower().Contains(keyword) ||
                    (s.User.Phone != null && s.User.Phone.Contains(keyword)) ||
                    s.User.Email.ToLower().Contains(keyword));
            }

            // Filter by position
            if (!string.IsNullOrWhiteSpace(position))
            {
                query = query.Where(s => s.Positions.Any(p => p.PositionName.ToLower().Contains(position.ToLower())));
            }

            // Filter by status
            if (status.HasValue)
            {
                query = query.Where(s => s.Status == status.Value);
            }

            // Get total count before pagination
            var totalCount = await query.CountAsync(ct);

            return (query, totalCount);
        }

        /// <summary>
        /// Get staff by ID with all related data
        /// </summary>
        public async Task<Staff?> GetStaffByIdAsync(int staffId, CancellationToken ct = default)
        {
            return await _context.Staffs
                .Include(s => s.User)
                    .ThenInclude(u => u.Role)
                .Include(s => s.Department)
                .Include(s => s.Positions)
                .FirstOrDefaultAsync(s => s.StaffId == staffId &&
                                         (s.User == null || s.User.IsDeleted != true), ct);
        }

        /// <summary>
        /// Get staff by User ID
        /// </summary>
        public async Task<Staff?> GetStaffByUserIdAsync(int userId, CancellationToken ct = default)
        {
            return await _context.Staffs
                .Include(s => s.User)
                    .ThenInclude(u => u.Role)
                .Include(s => s.Department)
                .Include(s => s.Positions)
                .FirstOrDefaultAsync(s => s.UserId == userId &&
                                         (s.User == null || s.User.IsDeleted != true), ct);
        }

        /// <summary>
        /// Check if email already exists
        /// </summary>
        public async Task<bool> EmailExistsAsync(string email, int? excludeUserId = null, CancellationToken ct = default)
        {
            var query = _context.Users
                .Where(u => u.Email.ToLower() == email.ToLower() && u.IsDeleted != true);

            if (excludeUserId.HasValue)
            {
                query = query.Where(u => u.UserId != excludeUserId.Value);
            }

            return await query.AnyAsync(ct);
        }

        /// <summary>
        /// Create new staff
        /// </summary>
        public async Task<Staff> CreateStaffAsync(Staff staff, CancellationToken ct = default)
        {
            _context.Staffs.Add(staff);
            await _context.SaveChangesAsync(ct);
            return staff;
        }

        /// <summary>
        /// Update existing staff
        /// </summary>
        public async Task<bool> UpdateStaffAsync(Staff staff, CancellationToken ct = default)
        {
            var existingStaff = await _context.Staffs
                .Include(s => s.User)
                .Include(s => s.Positions)
                .FirstOrDefaultAsync(s => s.StaffId == staff.StaffId, ct);

            if (existingStaff == null || existingStaff.User == null)
                return false;

            // Update staff fields
            existingStaff.SalaryBase = staff.SalaryBase;
            existingStaff.Status = staff.Status;

            // Update user fields
            existingStaff.User.FullName = staff.User.FullName;
            existingStaff.User.Phone = staff.User.Phone;
            existingStaff.User.AvatarUrl = staff.User.AvatarUrl;
            existingStaff.User.ModifiedAt = DateTime.UtcNow;
            existingStaff.User.ModifiedBy = staff.User.ModifiedBy;

            // Update positions (clear and re-add)
            existingStaff.Positions.Clear();
            foreach (var position in staff.Positions)
            {
                var pos = await _context.Positions.FindAsync(new object[] { position.PositionId }, ct);
                if (pos != null)
                {
                    existingStaff.Positions.Add(pos);
                }
            }

            _context.Staffs.Update(existingStaff);
            await _context.SaveChangesAsync(ct);

            return true;
        }

        /// <summary>
        /// Deactivate staff (set inactive status)
        /// </summary>
        public async Task<bool> DeactivateStaffAsync(int staffId, string? reason, CancellationToken ct = default)
        {
            var staff = await _context.Staffs
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.StaffId == staffId, ct);

            if (staff == null || staff.User == null)
                return false;

            // Mark staff as inactive (0 = Active, 1 = Inactive)
            staff.Status = 1;

            // Keep user record (do NOT soft-delete). Just mark user as inactive as well.
            staff.User.Status = 1;
            staff.User.ModifiedAt = DateTime.UtcNow;

            _context.Staffs.Update(staff);
            await _context.SaveChangesAsync(ct);

            return true;
        }

        /// <summary>
        /// Change staff status (0 = Active, 1 = Inactive)
        /// </summary>
        public async Task<bool> ChangeStaffStatusAsync(int staffId, int status, CancellationToken ct = default)
        {
            var staff = await _context.Staffs
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.StaffId == staffId &&
                                         (s.User == null || s.User.IsDeleted != true), ct);

            if (staff == null || staff.User == null)
                return false;

            staff.Status = status;
            staff.User.Status = status;
            staff.User.ModifiedAt = DateTime.UtcNow;

            _context.Staffs.Update(staff);
            await _context.SaveChangesAsync(ct);

            return true;
        }

        /// <summary>
        /// Check if staff exists and is not deleted
        /// </summary>
        public async Task<bool> StaffExistsAsync(int staffId, CancellationToken ct = default)
        {
            return await _context.Staffs
                .AnyAsync(s => s.StaffId == staffId &&
                             (s.User == null || s.User.IsDeleted != true), ct);
        }

        /// <summary>
        /// Get all active positions
        /// </summary>
        public async Task<List<Position>> GetActivePositionsAsync(CancellationToken ct = default)
        {
            return await _context.Positions
                .Where(p => p.Status == 0) // 0 = Active, 1 = Inactive
                .OrderBy(p => p.PositionName)
                .ToListAsync(ct);
        }

        /// <summary>
        /// Get staff count in department
        /// </summary>
        public async Task<int> GetStaffCountInDepartmentAsync(int departmentId, CancellationToken ct = default)
        {
            return await _context.Staffs
                .Where(s => s.DepartmentId == departmentId &&
                           (s.User == null || s.User.IsDeleted != true))
                .CountAsync(ct);
        }

        /// <summary>
        /// Add position to staff (insert into StaffPosition junction table)
        /// </summary>
        public async Task AddStaffPositionAsync(int staffId, int positionId, CancellationToken ct = default)
        {
            var staff = await _context.Staffs
                .Include(s => s.Positions)
                .FirstOrDefaultAsync(s => s.StaffId == staffId, ct);

            if (staff == null)
                throw new InvalidOperationException($"Staff with ID {staffId} not found.");

            var position = await _context.Positions.FindAsync(new object[] { positionId }, ct);

            if (position == null)
                throw new InvalidOperationException($"Position with ID {positionId} not found.");

            // Clear existing positions (enforce 1 position only)
            staff.Positions.Clear();
            
            // Add the single position
            staff.Positions.Add(position);
            
            await _context.SaveChangesAsync(ct);
        }
    }
}

