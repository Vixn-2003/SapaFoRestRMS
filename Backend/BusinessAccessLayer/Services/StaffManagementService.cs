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
                StatusText = s.Status == 0 ? "Đang hoạt động" : "Ngừng hoạt động", // 0 = Active, 1 = Inactive
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
                StatusText = staff.Status == 0 ? "Đang hoạt động" : "Ngừng hoạt động", // 0 = Active, 1 = Inactive
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
                Status = 0, // Active (0 = Active, 1 = Inactive)
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdBy,
                IsDeleted = false
            };

            // Create Staff entity
            var staff = new Staff
            {
                User = user,
                DepartmentId = null, // Department is not used anymore
                HireDate = dto.HireDate,
                SalaryBase = dto.BaseSalary,
                Status = 0 // Active (0 = Active, 1 = Inactive)
            };

            // Validate position exists
            var position = await _unitOfWork.Positions.GetByIdAsync(dto.PositionId);
            if (position == null)
            {
                return (false, null, "Invalid position selected.");
            }

            // Save to database
            try
            {
                // Save user and staff first
                var createdStaff = await _unitOfWork.StaffManagement.CreateStaffAsync(staff, ct);

                // Then add position relationship (includes SaveChanges internally)
                await _unitOfWork.StaffManagement.AddStaffPositionAsync(createdStaff.StaffId, dto.PositionId, ct);

                // Log to AuditLog
                var metadata = JsonSerializer.Serialize(new
                {
                    StaffId = createdStaff.StaffId,
                    UserId = createdStaff.UserId,
                    FullName = dto.FullName,
                    Email = dto.Email,
                    PositionId = dto.PositionId,
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
                PositionId = existingStaff.Positions.FirstOrDefault()?.PositionId
            };

            // Validate position exists
            var position = await _unitOfWork.Positions.GetByIdAsync(dto.PositionId);
            if (position == null)
            {
                return (false, "Invalid position selected.");
            }

            // Update staff entity
            existingStaff.User.FullName = dto.FullName;
            existingStaff.User.Phone = dto.Phone;
            existingStaff.User.AvatarUrl = dto.AvatarUrl;
            existingStaff.User.ModifiedAt = DateTime.UtcNow;
            existingStaff.User.ModifiedBy = modifiedBy;
            existingStaff.SalaryBase = dto.BaseSalary;
            existingStaff.Status = dto.Status;

            // Save changes
            try
            {
                var success = await _unitOfWork.StaffManagement.UpdateStaffAsync(existingStaff, ct);
                if (!success)
                {
                    return (false, "Failed to update staff.");
                }

                // Update position relationship (includes SaveChanges internally)
                await _unitOfWork.StaffManagement.AddStaffPositionAsync(existingStaff.StaffId, dto.PositionId, ct);

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
                        dto.PositionId
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

                return (true, "Cập nhật nhân viên thành công.");
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi khi cập nhật nhân viên: {ex.Message}");
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
                return (false, "Không tìm thấy nhân viên.");
            }

            // Business rule: Cannot deactivate yourself (if needed)
            // Business rule: Cannot deactivate manager or higher role (if needed)

            // Deactivate staff
            try
            {
                var success = await _unitOfWork.StaffManagement.DeactivateStaffAsync(dto.StaffId, dto.Reason, ct);
                if (!success)
                {
                    return (false, "Không thể ngừng hoạt động nhân viên.");
                }

                // (Không xoá user nữa) - chỉ ghi nhận người thực hiện vào ModifiedBy nếu có
                if (staff.User != null)
                {
                    staff.User.ModifiedBy = deletedBy;
                    await _unitOfWork.SaveChangesAsync();
                }

                // Log to AuditLog
                var metadata = JsonSerializer.Serialize(new
                {
                    StaffId = dto.StaffId,
                    StaffName = staff.User?.FullName ?? "Không rõ",
                    Reason = dto.Reason ?? "Không có",
                    DeletedBy = deletedBy
                });

                await _auditLogService.LogEventAsync(
                    eventType: "staff_deactivated",
                    entityType: "Staff",
                    entityId: dto.StaffId,
                    description: $"Quản lý {deletedBy} đã ngừng hoạt động nhân viên {staff.User?.FullName} (ID: {dto.StaffId})",
                    metadata: metadata,
                    userId: deletedBy,
                    ipAddress: ipAddress,
                    ct: ct
                );

                return (true, "Ngừng hoạt động nhân viên thành công.");
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi khi ngừng hoạt động nhân viên: {ex.Message}");
            }
        }

        /// <summary>
        /// Change staff status (0 = Active, 1 = Inactive)
        /// </summary>
        public async Task<(bool Success, string Message)> ChangeStatusAsync(
            int staffId,
            int status,
            int modifiedBy,
            string? ipAddress = null,
            CancellationToken ct = default)
        {
            if (status != 0 && status != 1)
            {
                return (false, "Trạng thái không hợp lệ.");
            }

            var staff = await _unitOfWork.StaffManagement.GetStaffByIdAsync(staffId, ct);
            if (staff == null)
            {
                return (false, "Không tìm thấy nhân viên.");
            }

            try
            {
                var success = await _unitOfWork.StaffManagement.ChangeStaffStatusAsync(staffId, status, ct);
                if (!success)
                {
                    return (false, "Không thể thay đổi trạng thái nhân viên.");
                }

                // Log to AuditLog
                var metadata = JsonSerializer.Serialize(new
                {
                    StaffId = staffId,
                    StaffName = staff.User?.FullName ?? "Không rõ",
                    Status = status,
                    ModifiedBy = modifiedBy
                });

                await _auditLogService.LogEventAsync(
                    eventType: "staff_status_changed",
                    entityType: "Staff",
                    entityId: staffId,
                    description: $"Quản lý {modifiedBy} đã {(status == 0 ? "kích hoạt" : "ngừng hoạt động")} nhân viên {staff.User?.FullName} (ID: {staffId})",
                    metadata: metadata,
                    userId: modifiedBy,
                    ipAddress: ipAddress,
                    ct: ct
                );

                return (true, status == 0 ? "Kích hoạt nhân viên thành công." : "Ngừng hoạt động nhân viên thành công.");
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi khi thay đổi trạng thái nhân viên: {ex.Message}");
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

