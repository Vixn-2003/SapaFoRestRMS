using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BusinessAccessLayer.DTOs.UserManagement;
using BusinessAccessLayer.Services.Interfaces;
using DataAccessLayer.Dbcontext;
using DataAccessLayer.UnitOfWork.Interfaces;
using DomainAccessLayer.Common;
using DomainAccessLayer.Models;
using Microsoft.EntityFrameworkCore;

namespace BusinessAccessLayer.Services
{
    public class UserManagementService : IUserManagementService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly SapaFoRestRmsContext _context;

        public UserManagementService(IUnitOfWork unitOfWork, SapaFoRestRmsContext context)
        {
            _unitOfWork = unitOfWork;
            _context = context;
        }

        public async Task<(int userId, string tempPassword)> CreateManagerAsync(CreateManagerRequest request, int adminUserId, CancellationToken ct = default)
        {
            // Role check: adminUserId must be Admin
            var admin = await _unitOfWork.Users.GetByIdAsync(adminUserId);
            if (admin == null)
                throw new UnauthorizedAccessException("Only admin can create manager accounts");

            var adminRoleName = await _context.Roles.Where(r => r.RoleId == admin.RoleId).Select(r => r.RoleName).FirstOrDefaultAsync(ct);
            if (!string.Equals(adminRoleName, "Admin", StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException("Only admin can create manager accounts");

            if (await _unitOfWork.Users.IsEmailExistsAsync(request.Email))
                throw new InvalidOperationException("Email already exists");

            int roleId;
            if (request.RoleId.HasValue)
            {
                roleId = request.RoleId.Value;
            }
            else
            {
                var managerRole = await _context.Roles.Where(r => r.RoleName == "Manager").Select(r => r.RoleId).FirstOrDefaultAsync(ct);
                if (managerRole == 0) throw new InvalidOperationException("Manager role not found");
                roleId = managerRole;
            }

            var tempPassword = PasswordGenerator.Generate();
            var passwordHash = HashPassword(tempPassword);

            await using var trx = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var user = new User
                {
                    FullName = request.FullName,
                    Email = request.Email,
                    Phone = request.Phone,
                    PasswordHash = passwordHash,
                    RoleId = roleId,
                    Status = 0,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = adminUserId,
                    IsDeleted = false
                };
                await _unitOfWork.Users.AddAsync(user);
                await _unitOfWork.SaveChangesAsync();

                await _unitOfWork.CommitAsync();
                return (user.UserId, tempPassword);
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        public async Task<(int userId, int staffId, string tempPassword)> CreateStaffAsync(CreateStaffRequest request, int managerUserId, CancellationToken ct = default)
        {
            // Role check: managerUserId must be Manager
            var manager = await _unitOfWork.Users.GetByIdAsync(managerUserId);
            if (manager == null)
                throw new UnauthorizedAccessException("Only manager can create staff accounts");

            var managerRoleName = await _context.Roles.Where(r => r.RoleId == manager.RoleId).Select(r => r.RoleName).FirstOrDefaultAsync(ct);
            if (!string.Equals(managerRoleName, "Manager", StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException("Only manager can create staff accounts");

            if (await _unitOfWork.Users.IsEmailExistsAsync(request.Email))
                throw new InvalidOperationException("Email already exists");

            int roleId;
            if (request.RoleId.HasValue)
            {
                roleId = request.RoleId.Value;
            }
            else
            {
                var staffRole = await _context.Roles.Where(r => r.RoleName == "Staff").Select(r => r.RoleId).FirstOrDefaultAsync(ct);
                if (staffRole == 0) throw new InvalidOperationException("Staff role not found");
                roleId = staffRole;
            }

            // Validate positions if provided
            var positions = new List<Position>();
            if (request.PositionIds.Any())
            {
                positions = await _context.Positions
                    .Where(p => request.PositionIds.Contains(p.PositionId))
                    .ToListAsync(ct);
                if (positions.Count != request.PositionIds.Count)
                    throw new InvalidOperationException("One or more positions not found");
            }

            var tempPassword = PasswordGenerator.Generate();
            var passwordHash = HashPassword(tempPassword);

            await using var trx = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var user = new User
                {
                    FullName = request.FullName,
                    Email = request.Email,
                    Phone = request.Phone,
                    PasswordHash = passwordHash,
                    RoleId = roleId,
                    Status = 0,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = managerUserId,
                    IsDeleted = false
                };
                await _unitOfWork.Users.AddAsync(user);
                await _unitOfWork.SaveChangesAsync();

                var staff = new Staff
                {
                    UserId = user.UserId,
                    HireDate = request.HireDate,
                    SalaryBase = request.SalaryBase,
                    Status = 0
                };

                // attach positions
                foreach (var pos in positions)
                {
                    staff.Positions.Add(pos);
                }

                await _context.Staffs.AddAsync(staff, ct);
                await _unitOfWork.SaveChangesAsync();

                await _unitOfWork.CommitAsync();
                return (user.UserId, staff.StaffId, tempPassword);
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        private static string HashPassword(string password)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }
    }
}


