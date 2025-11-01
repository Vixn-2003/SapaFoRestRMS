using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using BusinessAccessLayer.Common.Pagination;
using BusinessAccessLayer.DTOs.Users;
using BusinessAccessLayer.Services.Interfaces;
using DataAccessLayer.Dbcontext;
using DataAccessLayer.UnitOfWork.Interfaces;
using DomainAccessLayer.Models;
using Microsoft.EntityFrameworkCore;

namespace BusinessAccessLayer.Services
{
    public class UserService : IUserService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly SapaFoRestRmsContext _context;

        public UserService(IUnitOfWork unitOfWork, IMapper mapper, SapaFoRestRmsContext context)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _context = context;
        }

        public async Task<IEnumerable<UserDto>> GetAllAsync(CancellationToken ct = default)
        {
            var users = await _unitOfWork.Users.GetAllAsync();
            var activeUsers = users.Where(u => u.IsDeleted == false).ToList();

            var userDtos = new List<UserDto>();
            foreach (var user in activeUsers)
            {
                var userDto = _mapper.Map<UserDto>(user);
                // Load Role name
                var role = await _context.Roles.FindAsync(new object[] { user.RoleId }, ct);
                userDto.RoleName = role?.RoleName ?? "Unknown";
                userDtos.Add(userDto);
            }

            return userDtos;
        }

        public async Task<UserDto?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(id);
            if (user == null || user.IsDeleted == true)
            {
                return null;
            }

            var userDto = _mapper.Map<UserDto>(user);
            // Load Role name
            var role = await _context.Roles.FindAsync(new object[] { user.RoleId }, ct);
            userDto.RoleName = role?.RoleName ?? "Unknown";

            return userDto;
        }

        public async Task<UserListResponse> SearchAsync(UserSearchRequest request, CancellationToken ct = default)
        {
            // Start with base query - only non-deleted users
            var query = _context.Users
                .Include(u => u.Role)
                .Where(u => u.IsDeleted == false)
                .AsQueryable();

            // Apply search term (search in FullName, Email, Phone)
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var searchTerm = request.SearchTerm.Trim().ToLower();
                query = query.Where(u =>
                    (u.FullName != null && u.FullName.ToLower().Contains(searchTerm)) ||
                    (u.Email != null && u.Email.ToLower().Contains(searchTerm)) ||
                    (u.Phone != null && u.Phone.ToLower().Contains(searchTerm))
                );
            }

            // Apply RoleId filter
            if (request.RoleId.HasValue)
            {
                query = query.Where(u => u.RoleId == request.RoleId.Value);
            }

            // Apply Status filter
            if (request.Status.HasValue)
            {
                query = query.Where(u => u.Status == request.Status.Value);
            }

            // Apply sorting
            var sortBy = string.IsNullOrWhiteSpace(request.SortBy) ? "FullName" : request.SortBy;
            var sortOrder = string.IsNullOrWhiteSpace(request.SortOrder) ? "asc" : request.SortOrder.ToLower();

            query = sortOrder == "desc" ? sortBy switch
            {
                "FullName" => query.OrderByDescending(u => u.FullName),
                "Email" => query.OrderByDescending(u => u.Email),
                "Phone" => query.OrderByDescending(u => u.Phone),
                "RoleId" => query.OrderByDescending(u => u.RoleId),
                "Status" => query.OrderByDescending(u => u.Status),
                "CreatedAt" => query.OrderByDescending(u => u.CreatedAt),
                _ => query.OrderByDescending(u => u.FullName)
            } : sortBy switch
            {
                "FullName" => query.OrderBy(u => u.FullName),
                "Email" => query.OrderBy(u => u.Email),
                "Phone" => query.OrderBy(u => u.Phone),
                "RoleId" => query.OrderBy(u => u.RoleId),
                "Status" => query.OrderBy(u => u.Status),
                "CreatedAt" => query.OrderBy(u => u.CreatedAt),
                _ => query.OrderBy(u => u.FullName)
            };

            // Get total count before pagination
            var totalCount = await query.CountAsync(ct);

            // Apply pagination
            var page = request.Page <= 0 ? 1 : request.Page;
            var pageSize = request.PageSize <= 0 ? 10 : request.PageSize;
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var users = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            // Map to DTOs
            var userDtos = users.Select(user =>
            {
                var userDto = _mapper.Map<UserDto>(user);
                userDto.RoleName = user.Role?.RoleName ?? "Unknown";
                return userDto;
            }).ToList();

            return new UserListResponse
            {
                Users = userDtos,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages,
                HasPreviousPage = page > 1,
                HasNextPage = page < totalPages
            };
        }

        public async Task<UserDto> CreateAsync(UserCreateRequest request, CancellationToken ct = default)
        {
            // Business validation
            if (await _unitOfWork.Users.IsEmailExistsAsync(request.Email))
            {
                throw new InvalidOperationException("Email already exists");
            }

            // Hash password
            var passwordHash = HashPassword(request.Password);

            // Map request to User entity
            var user = new User
            {
                FullName = request.FullName,
                Email = request.Email,
                Phone = request.Phone,
                PasswordHash = passwordHash,
                RoleId = request.RoleId,
                Status = request.Status,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            await _unitOfWork.Users.AddAsync(user);
            await _unitOfWork.SaveChangesAsync();

            // Map to DTO for response
            var userDto = _mapper.Map<UserDto>(user);
            var role = await _context.Roles.FindAsync(new object[] { user.RoleId }, ct);
            userDto.RoleName = role?.RoleName ?? "Unknown";

            return userDto;
        }

        public async Task UpdateAsync(int id, UserUpdateRequest request, CancellationToken ct = default)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(id);
            if (user == null || user.IsDeleted == true)
            {
                throw new InvalidOperationException("User not found");
            }

            // Check if email is being changed and if new email already exists
            if (user.Email != request.Email && await _unitOfWork.Users.IsEmailExistsAsync(request.Email))
            {
                throw new InvalidOperationException("Email already exists");
            }

            // Update user properties
            user.FullName = request.FullName;
            user.Email = request.Email;
            user.Phone = request.Phone;
            user.RoleId = request.RoleId;
            user.Status = request.Status;
            user.ModifiedAt = DateTime.UtcNow;

            await _unitOfWork.Users.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id, CancellationToken ct = default)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(id);
            if (user == null || user.IsDeleted == true)
            {
                throw new InvalidOperationException("User not found");
            }

            // Soft delete
            user.IsDeleted = true;
            user.DeletedAt = DateTime.UtcNow;

            await _unitOfWork.Users.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task ChangeStatusAsync(int id, int status, CancellationToken ct = default)
        {
            if (status < 0 || status > 2)
            {
                throw new ArgumentException("Status must be between 0 and 2");
            }

            var user = await _unitOfWork.Users.GetByIdAsync(id);
            if (user == null || user.IsDeleted == true)
            {
                throw new InvalidOperationException("User not found");
            }

            user.Status = status;
            user.ModifiedAt = DateTime.UtcNow;

            await _unitOfWork.Users.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();
        }

        private static string HashPassword(string password)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }
    }
}

