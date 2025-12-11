using AutoMapper;
using BusinessAccessLayer.Common.Pagination;
using BusinessAccessLayer.DTOs.Positions;
using BusinessAccessLayer.DTOs.Staff;
using BusinessAccessLayer.Services.Interfaces;
using DataAccessLayer.UnitOfWork.Interfaces;
using DomainAccessLayer.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace BusinessAccessLayer.Services
{
    /// <summary>
    /// Service implementation for Staff Management business logic
    /// </summary>
    public class StaffManagementService : IStaffManagementService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IAuditLogService _auditLogService;
        private readonly IConfiguration _configuration;

        public StaffManagementService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IAuditLogService auditLogService,
            IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _auditLogService = auditLogService;
            _configuration = configuration;
        }

        /// <summary>
        /// UC55 - Get paginated staff list with filters
        /// </summary>
        public async Task<PagedResult<StaffListItemDto>> GetStaffListAsync(
            StaffFilterDto filter,
            int? managerDepartmentId = null,
            CancellationToken ct = default)
        {
            // Validate filter
            if (filter.Page <= 0) filter.Page = 1;
            if (filter.PageSize <= 0 || filter.PageSize > 100) filter.PageSize = 20;

            // If manager has department, filter by their department
            var departmentId = managerDepartmentId ?? filter.DepartmentId;

            // Get staff query from repository
            var (query, totalCount) = await _unitOfWork.StaffManagement.GetStaffQueryAsync(
                departmentId,
                filter.SearchKeyword,
                filter.Position,
                filter.Status,
                filter.SortBy,
                filter.SortDirection,
                ct);

            // Apply sorting
            query = filter.SortBy?.ToLower() switch
            {
                "name" => filter.SortDirection?.ToLower() == "asc"
                    ? query.OrderBy(s => s.User.FullName)
                    : query.OrderByDescending(s => s.User.FullName),
                "basesalary" => filter.SortDirection?.ToLower() == "asc"
                    ? query.OrderBy(s => s.SalaryBase)
                    : query.OrderByDescending(s => s.SalaryBase),
                "hiredate" => filter.SortDirection?.ToLower() == "asc"
                    ? query.OrderBy(s => s.HireDate)
                    : query.OrderByDescending(s => s.HireDate),
                _ => query.OrderByDescending(s => s.HireDate)
            };

            // Apply pagination
            var pageSize = filter.PageSize;
            var page = filter.Page;
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var staffList = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .AsEnumerable() // Execute query
                .ToList();

            // Map to DTOs
            var items = staffList.Select(s => new StaffListItemDto
            {
                StaffId = s.StaffId,
                UserId = s.UserId,
                FullName = s.User?.FullName ?? "N/A",
                Phone = s.User?.Phone,
                Email = s.User?.Email ?? "",
                AvatarUrl = s.User?.AvatarUrl,
                Positions = string.Join(", ", s.Positions.Select(p => p.PositionName)),
                BaseSalary = s.SalaryBase,
                Status = s.Status,
                StatusText = s.Status == 1 ? "Active" : "Inactive",
                HireDate = s.HireDate,
                DepartmentName = s.Department?.Name,
                DepartmentId = s.DepartmentId
            }).ToList();

            return new PagedResult<StaffListItemDto>
            {
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages,
                Data = items
            };
        }

        /// <summary>
        /// Get staff detail by ID
        /// </summary>
        public async Task<StaffDetailDto?> GetStaffDetailAsync(int staffId, CancellationToken ct = default)
        {
            var staff = await _unitOfWork.StaffManagement.GetStaffByIdAsync(staffId, ct);
            if (staff == null || staff.User == null)
                return null;

            var dto = new StaffDetailDto
            {
                StaffId = staff.StaffId,
                UserId = staff.UserId,
                FullName = staff.User.FullName,
                Email = staff.User.Email,
                Phone = staff.User.Phone,
                AvatarUrl = staff.User.AvatarUrl,
                HireDate = staff.HireDate,
                BaseSalary = staff.SalaryBase,
                Status = staff.Status,
                StatusText = staff.Status == 1 ? "Active" : "Inactive",
                DepartmentId = staff.DepartmentId,
                DepartmentName = staff.Department?.Name,
                RoleId = staff.User.RoleId,
                RoleName = staff.User.Role?.RoleName ?? "Unknown",
                CreatedAt = staff.User.CreatedAt,
                ModifiedAt = staff.User.ModifiedAt,
                Positions = staff.Positions.Select(p => new StaffPositionDto
                {
                    PositionId = p.PositionId,
                    PositionName = p.PositionName
                }).ToList()
            };

            return dto;
        }

        /// <summary>
        /// Create new staff
        /// </summary>
        public async Task<(bool Success, int? StaffId, string Message)> CreateStaffAsync(
            StaffCreateDto dto,
            int createdBy,
            string? ipAddress = null,
            CancellationToken ct = default)
        {
            // Validate email uniqueness
            if (await _unitOfWork.StaffManagement.EmailExistsAsync(dto.Email, null, ct))
            {
                return (false, null, "Email already exists in the system.");
            }

            // Generate password if not provided
            var password = string.IsNullOrWhiteSpace(dto.Password)
                ? GenerateRandomPassword()
                : dto.Password;

            var passwordHash = HashPassword(password);

            // Create User entity
            var user = new User
            {
                FullName = dto.FullName,
                Email = dto.Email,
                Phone = dto.Phone,
                PasswordHash = passwordHash,
                RoleId = dto.RoleId,
                AvatarUrl = dto.AvatarUrl,
                Status = 1, // Active
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdBy,
                IsDeleted = false
            };

            // Create Staff entity
            var staff = new Staff
            {
                User = user,
                DepartmentId = dto.DepartmentId,
                HireDate = dto.HireDate,
                SalaryBase = dto.BaseSalary,
                Status = 1 // Active
            };

            // Add positions
            var positions = await _unitOfWork.Positions.GetByIdsAsync(dto.PositionIds);
            if (positions.Count != dto.PositionIds.Count)
            {
                return (false, null, "One or more positions are invalid.");
            }

            staff.Positions = positions;

            // Save to database
            try
            {
                var createdStaff = await _unitOfWork.StaffManagement.CreateStaffAsync(staff, ct);

                // Log to AuditLog
                var metadata = JsonSerializer.Serialize(new
                {
                    StaffId = createdStaff.StaffId,
                    UserId = createdStaff.UserId,
                    FullName = dto.FullName,
                    Email = dto.Email,
                    DepartmentId = dto.DepartmentId,
                    PositionIds = dto.PositionIds,
                    BaseSalary = dto.BaseSalary,
                    CreatedBy = createdBy
                });

                await _auditLogService.LogEventAsync(
                    eventType: "staff_created",
                    entityType: "Staff",
                    entityId: createdStaff.StaffId,
                    description: $"Manager {createdBy} created new staff {dto.FullName} (ID: {createdStaff.StaffId})",
                    metadata: metadata,
                    userId: createdBy,
                    ipAddress: ipAddress,
                    ct: ct
                );

                return (true, createdStaff.StaffId, $"Staff created successfully. Password: {password}");
            }
            catch (Exception ex)
            {
                return (false, null, $"Error creating staff: {ex.Message}");
            }
        }

        /// <summary>
        /// UC56 - Update existing staff
        /// </summary>
        public async Task<(bool Success, string Message)> UpdateStaffAsync(
            StaffUpdateDto dto,
            int modifiedBy,
            string? ipAddress = null,
            CancellationToken ct = default)
        {
            // Get existing staff
            var existingStaff = await _unitOfWork.StaffManagement.GetStaffByIdAsync(dto.StaffId, ct);
            if (existingStaff == null || existingStaff.User == null)
            {
                return (false, "Staff not found.");
            }

            // Store old values for audit log
            var oldValues = new
            {
                FullName = existingStaff.User.FullName,
                Phone = existingStaff.User.Phone,
                BaseSalary = existingStaff.SalaryBase,
                Status = existingStaff.Status,
                Positions = existingStaff.Positions.Select(p => p.PositionId).ToList()
            };

            // Get positions
            var positions = await _unitOfWork.Positions.GetByIdsAsync(dto.PositionIds);
            if (positions.Count != dto.PositionIds.Count)
            {
                return (false, "One or more positions are invalid.");
            }

            // Update staff entity
            existingStaff.User.FullName = dto.FullName;
            existingStaff.User.Phone = dto.Phone;
            existingStaff.User.AvatarUrl = dto.AvatarUrl;
            existingStaff.User.ModifiedAt = DateTime.UtcNow;
            existingStaff.User.ModifiedBy = modifiedBy;
            existingStaff.SalaryBase = dto.BaseSalary;
            existingStaff.Status = dto.Status;
            existingStaff.Positions = positions;

            // Save changes
            try
            {
                var success = await _unitOfWork.StaffManagement.UpdateStaffAsync(existingStaff, ct);
                if (!success)
                {
                    return (false, "Failed to update staff.");
                }

                // Log to AuditLog
                var metadata = JsonSerializer.Serialize(new
                {
                    StaffId = dto.StaffId,
                    OldValues = oldValues,
                    NewValues = new
                    {
                        dto.FullName,
                        dto.Phone,
                        dto.BaseSalary,
                        dto.Status,
                        dto.PositionIds
                    },
                    ModifiedBy = modifiedBy
                });

                await _auditLogService.LogEventAsync(
                    eventType: "staff_updated",
                    entityType: "Staff",
                    entityId: dto.StaffId,
                    description: $"Manager {modifiedBy} updated staff {dto.FullName} (ID: {dto.StaffId})",
                    metadata: metadata,
                    userId: modifiedBy,
                    ipAddress: ipAddress,
                    ct: ct
                );

                return (true, "Staff updated successfully.");
            }
            catch (Exception ex)
            {
                return (false, $"Error updating staff: {ex.Message}");
            }
        }

        /// <summary>
        /// UC57 - Deactivate staff (soft delete)
        /// </summary>
        public async Task<(bool Success, string Message)> DeactivateStaffAsync(
            StaffDeactivateDto dto,
            int deletedBy,
            string? ipAddress = null,
            CancellationToken ct = default)
        {
            // Check if staff exists
            var staff = await _unitOfWork.StaffManagement.GetStaffByIdAsync(dto.StaffId, ct);
            if (staff == null)
            {
                return (false, "Staff not found.");
            }

            // Business rule: Cannot deactivate yourself (if needed)
            // Business rule: Cannot deactivate manager or higher role (if needed)

            // Deactivate staff
            try
            {
                var success = await _unitOfWork.StaffManagement.DeactivateStaffAsync(dto.StaffId, dto.Reason, ct);
                if (!success)
                {
                    return (false, "Failed to deactivate staff.");
                }

                // Update DeletedBy in User
                if (staff.User != null)
                {
                    staff.User.DeletedBy = deletedBy;
                    await _unitOfWork.SaveChangesAsync();
                }

                // Log to AuditLog
                var metadata = JsonSerializer.Serialize(new
                {
                    StaffId = dto.StaffId,
                    StaffName = staff.User?.FullName ?? "Unknown",
                    Reason = dto.Reason ?? "No reason provided",
                    DeletedBy = deletedBy
                });

                await _auditLogService.LogEventAsync(
                    eventType: "staff_deactivated",
                    entityType: "Staff",
                    entityId: dto.StaffId,
                    description: $"Manager {deletedBy} deactivated staff {staff.User?.FullName} (ID: {dto.StaffId})",
                    metadata: metadata,
                    userId: deletedBy,
                    ipAddress: ipAddress,
                    ct: ct
                );

                return (true, "Staff deactivated successfully.");
            }
            catch (Exception ex)
            {
                return (false, $"Error deactivating staff: {ex.Message}");
            }
        }

        /// <summary>
        /// Get available positions for dropdown
        /// </summary>
        public async Task<List<PositionDto>> GetActivePositionsAsync(CancellationToken ct = default)
        {
            var positions = await _unitOfWork.StaffManagement.GetActivePositionsAsync(ct);
            return _mapper.Map<List<PositionDto>>(positions);
        }

        /// <summary>
        /// Validate if manager can manage this staff
        /// </summary>
        public async Task<bool> CanManagerManageStaffAsync(int managerId, int staffId, CancellationToken ct = default)
        {
            // Get manager's staff record to find their department
            var managerStaff = await _unitOfWork.StaffManagement.GetStaffByUserIdAsync(managerId, ct);
            if (managerStaff == null || !managerStaff.DepartmentId.HasValue)
                return false;

            // Get target staff
            var targetStaff = await _unitOfWork.StaffManagement.GetStaffByIdAsync(staffId, ct);
            if (targetStaff == null)
                return false;

            // Manager can only manage staff in their department
            return managerStaff.DepartmentId == targetStaff.DepartmentId;
        }

        // Helper methods
        private string GenerateRandomPassword(int length = 12)
        {
            const string validChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!@#$%^&*";
            var random = new Random();
            return new string(Enumerable.Range(0, length)
                .Select(_ => validChars[random.Next(validChars.Length)])
                .ToArray());
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }
    }
}

