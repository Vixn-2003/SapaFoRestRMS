using BusinessAccessLayer.DTOs.Auth;
using BusinessAccessLayer.Services.Interfaces;
using DataAccessLayer.Repositories.Interfaces;
using DomainAccessLayer.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using DomainAccessLayer.Common;
using DataAccessLayer.Dbcontext;

using Microsoft.EntityFrameworkCore;

namespace BusinessAccessLayer.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IConfiguration _configuration;
        public AuthService(IUserRepository userRepository, IConfiguration configuration)
        {
            _userRepository = userRepository;
            _configuration = configuration;
        }

       
        public async Task<LoginResponse> LoginAsync(LoginRequest request)
        {
            var user = await _userRepository.GetByEmailAsync(request.Email);
            if (user == null || !VerifyPassword(request.Password, user.PasswordHash))
                throw new UnauthorizedAccessException("Invalid email or password");

            if (user.IsDeleted == true)
                throw new UnauthorizedAccessException("This account has been deleted");

            return new LoginResponse
            {
                UserId = user.UserId,
                FullName = user.FullName ?? "",
                Email = user.Email,
                Role = (Role)user.Role,
                Token = GenerateJwtToken(user)
            };
        }

        public async Task<LoginResponse> RegisterAsync(RegisterRequest request)
        {
            if (await IsEmailExistsAsync(request.Email))
                throw new InvalidOperationException("Email already exists");

            var user = new User
            {
                FullName = request.FullName,
                Email = request.Email,
                PasswordHash = HashPassword(PasswordGenerator.Generate()),
                Phone = request.Phone,
                RoleId = (int)request.Role.RoleId,
              
                CreatedAt = DateTime.Now
            };

            await _userRepository.AddAsync(user);

            return new LoginResponse
            {
                UserId = user.UserId,
                FullName = user.FullName ?? "",
                Email = user.Email,
                Role = (Role)user.Role,
                Token = GenerateJwtToken(user)
            };
        }
        public async Task<(int userId, int staffId, string tempPassword)> RegisterStaffAsync(
    RegisterStaffRequest request,
    int managerUserId,
    SapaFoRestRmsContext context,
    CancellationToken ct = default)
        {
            // Enforce unique email on active users
           if(await IsEmailExistsAsync(request.Email))
            {
                throw new InvalidOperationException("Email already exists");
            }

            // Resolve staff role
            int roleId;
            if (request.RoleId.HasValue)
            {
                roleId = request.RoleId.Value;
            }
            else
            {
                var staffRole = await context.Roles
                    .Where(r => r.RoleName == "Staff")
                    .Select(r => new { r.RoleId })
                    .FirstOrDefaultAsync(ct);
                if (staffRole == null)
                    throw new InvalidOperationException("Staff role not found");
                roleId = staffRole.RoleId;
            }

            // Generate and hash a temporary password
            var tempPassword = DomainAccessLayer.Common.PasswordGenerator.Generate();
            string HashPassword(string password)
            {
                using var sha256 = System.Security.Cryptography.SHA256.Create();
                var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(bytes);
            }
            var passwordHash = HashPassword(tempPassword);

            await using var trx = await context.Database.BeginTransactionAsync(ct);
            try
            {
                var user = new DomainAccessLayer.Models.User
                {
                    FullName = request.FullName,
                    Email = request.Email,
                    Phone = request.Phone,
                    PasswordHash = passwordHash,
                    RoleId = roleId,
                    Status = request.UserStatus ?? 0,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = managerUserId,
                    IsDeleted = false
                };

                await context.Users.AddAsync(user, ct);
                await context.SaveChangesAsync(ct); // obtain UserId for FK

                var staff = new DomainAccessLayer.Models.Staff
                {
                    UserId = user.UserId,
                    HireDate = request.HireDate,
                    SalaryBase = request.SalaryBase,
                    Status = request.StaffStatus ?? 0
                };

                await context.Staffs.AddAsync(staff, ct);
                await context.SaveChangesAsync(ct);

                await trx.CommitAsync(ct);
                return (user.UserId, staff.StaffId, tempPassword);
            }
            catch
            {
                await trx.RollbackAsync(ct);
                throw;
            }
        }

        public async Task<bool> IsEmailExistsAsync(string email)
        {
            return await _userRepository.IsEmailExistsAsync(email);
        }
        private string GenerateJwtToken(User user)
        {
            var jwtConfig = _configuration.GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig["Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim("userId", user.UserId.ToString()),
                new Claim("email", user.Email),
                new Claim(System.Security.Claims.ClaimTypes.Role, ((Role)user.Role).ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: jwtConfig["Issuer"],
                audience: jwtConfig["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(Convert.ToDouble(jwtConfig["ExpireMinutes"])),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        private bool VerifyPassword(string password, string hash)
        {
            return HashPassword(password) == hash;
        }

    }
}
