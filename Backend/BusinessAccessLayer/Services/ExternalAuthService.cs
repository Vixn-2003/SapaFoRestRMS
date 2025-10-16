using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BusinessAccessLayer.DTOs.Auth;
using BusinessAccessLayer.Services.Interfaces;
using DataAccessLayer.Repositories.Interfaces;
using DomainAccessLayer.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Google.Apis.Auth;

namespace BusinessAccessLayer.Services
{
    public class ExternalAuthService : IExternalAuthService
    {
        private readonly IUserRepository _users;
        private readonly IConfiguration _configuration;

        public ExternalAuthService(IUserRepository users, IConfiguration configuration)
        {
            _users = users;
            _configuration = configuration;
        }

        public async Task<LoginResponse> GoogleLoginAsync(GoogleLoginRequest request, CancellationToken ct = default)
        {
            var clientId = _configuration["Google:ClientId"];
            var payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken, new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { clientId }
            });

            var email = payload.Email;
            var name = payload.Name ?? payload.Email;

            var user = await _users.GetByEmailAsync(email);
            if (user == null)
            {
                user = new User
                {
                    FullName = name,
                    Email = email,
                    PasswordHash = HashPassword(Guid.NewGuid().ToString()),
                    RoleId = await ResolveDefaultRoleIdAsync(),
                    Status = 0,
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false
                };
                await _users.AddAsync(user);
                await _users.SaveChangesAsync();
            }

            return new LoginResponse
            {
                UserId = user.UserId,
                FullName = user.FullName ?? string.Empty,
                Email = user.Email,
                RoleId = user.RoleId,
                RoleName = user.Role?.RoleName ?? string.Empty,
                Token = GenerateJwtToken(user)
            };
        }

        private async Task<int> ResolveDefaultRoleIdAsync()
        {
            // Default to Customer role; adjust as needed
            // For simplicity, assume Customer has RoleId = 3; otherwise query roles via context if available
            return 3;
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
                new Claim(ClaimTypes.Role, user.Role?.RoleName ?? user.RoleId.ToString()),
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

        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }
    }
}


