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
                RoleId = user.RoleId,
                RoleName = user.Role?.RoleName ?? string.Empty,
                Token = GenerateJwtToken(user),
                RefreshToken = GenerateRefreshToken(user)
            };
        }

        public Task<LoginResponse> RefreshTokenAsync(string refreshToken)
        {
            var principal = ValidateJwt(refreshToken, requireRefreshClaim: true);
            if (principal == null)
                throw new UnauthorizedAccessException("Invalid refresh token");

            var userIdClaim = principal.FindFirst("userId")?.Value ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                throw new UnauthorizedAccessException("Invalid refresh token payload");

            var user = _userRepository.GetByIdAsync(userId).GetAwaiter().GetResult();
            if (user == null || user.IsDeleted == true)
                throw new UnauthorizedAccessException("User not found");

            var response = new LoginResponse
            {
                UserId = user.UserId,
                FullName = user.FullName ?? string.Empty,
                Email = user.Email,
                RoleId = user.RoleId,
                RoleName = user.Role?.RoleName ?? string.Empty,
                Token = GenerateJwtToken(user),
                RefreshToken = GenerateRefreshToken(user)
            };
            return Task.FromResult(response);
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
                new Claim(System.Security.Claims.ClaimTypes.Role, user.Role?.RoleName ?? user.RoleId.ToString()),
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

        private string GenerateRefreshToken(User user)
        {
            var jwtConfig = _configuration.GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig["Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim("userId", user.UserId.ToString()),
                new Claim("rt", "1") // mark as refresh token
            };

            var days = 7;
            int.TryParse(jwtConfig["RefreshExpireDays"], out days);

            var token = new JwtSecurityToken(
                issuer: jwtConfig["Issuer"],
                audience: jwtConfig["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddDays(days > 0 ? days : 7),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private ClaimsPrincipal? ValidateJwt(string token, bool requireRefreshClaim)
        {
            var jwtConfig = _configuration.GetSection("Jwt");
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(jwtConfig["Key"]);
            try
            {
                var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidIssuer = jwtConfig["Issuer"],
                    ValidAudience = jwtConfig["Audience"],
                    ClockSkew = TimeSpan.Zero
                }, out var validatedToken);

                if (requireRefreshClaim)
                {
                    var hasRt = principal.HasClaim(c => c.Type == "rt");
                    if (!hasRt) return null;
                }

                return principal;
            }
            catch
            {
                return null;
            }
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
