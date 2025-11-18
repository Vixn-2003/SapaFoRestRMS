using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using BusinessAccessLayer.DTOs.Users;
using BusinessAccessLayer.Services.Interfaces;
using DataAccessLayer.Repositories.Interfaces;
using DataAccessLayer.UnitOfWork.Interfaces;
using DomainAccessLayer.Models;

namespace BusinessAccessLayer.Services
{
    public class UserService : IUserService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IRoleRepository _roleRepository;
        private readonly IEmailService _emailService;
        private static readonly HashSet<int> RestrictedCreationRoleIds = new() { 2, 5 };

        public UserService(IUnitOfWork unitOfWork, IMapper mapper, IRoleRepository roleRepository, IEmailService emailService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _roleRepository = roleRepository;
            _emailService = emailService;
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
                var role = await _roleRepository.GetByIdAsync(user.RoleId);
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
            var role = await _roleRepository.GetByIdAsync(user.RoleId);
            userDto.RoleName = role?.RoleName ?? "Unknown";

            return userDto;
        }

        public async Task<UserListResponse> SearchAsync(UserSearchRequest request, CancellationToken ct = default)
        {
            // Get all users from repository (already filtered by IsDeleted = false)
            var allUsers = await _unitOfWork.Users.GetAllAsync();
            var usersList = allUsers.ToList();

            // Apply search term (search in FullName, Email, Phone)
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var searchTerm = request.SearchTerm.Trim().ToLower();
                usersList = usersList.Where(u =>
                    (u.FullName != null && u.FullName.ToLower().Contains(searchTerm)) ||
                    (u.Email != null && u.Email.ToLower().Contains(searchTerm)) ||
                    (u.Phone != null && u.Phone.ToLower().Contains(searchTerm))
                ).ToList();
            }

            // Apply RoleId filter
            if (request.RoleId.HasValue)
            {
                usersList = usersList.Where(u => u.RoleId == request.RoleId.Value).ToList();
            }

            // Apply Status filter
            if (request.Status.HasValue)
            {
                usersList = usersList.Where(u => u.Status == request.Status.Value).ToList();
            }

            // Apply sorting
            var sortBy = string.IsNullOrWhiteSpace(request.SortBy) ? "FullName" : request.SortBy;
            var sortOrder = string.IsNullOrWhiteSpace(request.SortOrder) ? "asc" : request.SortOrder.ToLower();

            var sortedUsers = sortOrder == "desc" ? sortBy switch
            {
                "FullName" => usersList.OrderByDescending(u => u.FullName),
                "Email" => usersList.OrderByDescending(u => u.Email),
                "Phone" => usersList.OrderByDescending(u => u.Phone),
                "RoleId" => usersList.OrderByDescending(u => u.RoleId),
                "Status" => usersList.OrderByDescending(u => u.Status),
                "CreatedAt" => usersList.OrderByDescending(u => u.CreatedAt),
                _ => usersList.OrderByDescending(u => u.FullName)
            } : sortBy switch
            {
                "FullName" => usersList.OrderBy(u => u.FullName),
                "Email" => usersList.OrderBy(u => u.Email),
                "Phone" => usersList.OrderBy(u => u.Phone),
                "RoleId" => usersList.OrderBy(u => u.RoleId),
                "Status" => usersList.OrderBy(u => u.Status),
                "CreatedAt" => usersList.OrderBy(u => u.CreatedAt),
                _ => usersList.OrderBy(u => u.FullName)
            };

            // Get total count before pagination
            var totalCount = sortedUsers.Count();

            // Apply pagination
            var page = request.Page <= 0 ? 1 : request.Page;
            var pageSize = request.PageSize <= 0 ? 10 : request.PageSize;
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var paginatedUsers = sortedUsers
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Map to DTOs and load Role names
            var userDtos = new List<UserDto>();
            foreach (var user in paginatedUsers)
            {
                var userDto = _mapper.Map<UserDto>(user);
                var role = await _roleRepository.GetByIdAsync(user.RoleId);
                userDto.RoleName = role?.RoleName ?? "Unknown";
                userDtos.Add(userDto);
            }

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

            if (RestrictedCreationRoleIds.Contains(request.RoleId))
            {
                throw new InvalidOperationException("Không được phép tạo tài khoản Admin hoặc Customer bằng chức năng này");
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

            // Send credentials to user's email (best-effort)
            try
            {
                var subject = "Tài khoản SapaFoRestRMS đã được tạo";
                var body = $@"
<div style='font-family:Segoe UI,Helvetica,Arial,sans-serif;font-size:14px;'>
  <p>Chào {request.FullName},</p>
  <p>Tài khoản của bạn đã được tạo trên hệ thống SapaFoRest RMS.</p>
  <p><strong>Thông tin đăng nhập:</strong></p>
  <ul>
    <li>Email: <strong>{request.Email}</strong></li>
    <li>Mật khẩu tạm thời: <strong>{request.Password}</strong></li>
  </ul>
  <p>Vui lòng đăng nhập và đổi mật khẩu sau lần đăng nhập đầu tiên để đảm bảo an toàn.</p>
  <p>Trân trọng,</p>
  <p>SapaFoRest RMS</p>
  <hr />
  <small>Đây là email tự động, vui lòng không trả lời.</small>
</div>";
                await _emailService.SendAsync(request.Email, subject, body);
            }
            catch
            {
                // Intentionally swallow email errors to not block account creation
            }

            // Map to DTO for response
            var userDto = _mapper.Map<UserDto>(user);
            var role = await _roleRepository.GetByIdAsync(user.RoleId);
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

        public async Task<UserDto> UpdateProfileAsync(int id, UserProfileUpdateRequest request, CancellationToken ct = default)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(id);
            if (user == null || user.IsDeleted == true)
            {
                throw new InvalidOperationException("User not found");
            }

            // Only update profile fields (FullName, Phone, AvatarUrl if supported)
            user.FullName = request.FullName;
            if (!string.IsNullOrWhiteSpace(request.Phone))
            {
                user.Phone = request.Phone;
            }
            user.AvatarUrl = string.IsNullOrWhiteSpace(request.AvatarUrl)
                ? null
                : request.AvatarUrl.Trim();
            user.ModifiedAt = DateTime.UtcNow;

            await _unitOfWork.Users.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();

            // Return updated user DTO
            var userDto = _mapper.Map<UserDto>(user);
            var role = await _roleRepository.GetByIdAsync(user.RoleId);
            userDto.RoleName = role?.RoleName ?? "Unknown";

            return userDto;
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

