using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BusinessAccessLayer.DTOs.Auth;
using BusinessAccessLayer.Services.Interfaces;
using DataAccessLayer.Dbcontext;
using DataAccessLayer.Repositories.Interfaces;
using DomainAccessLayer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BusinessAccessLayer.Services
{
    public class PhoneAuthService : IPhoneAuthService
    {
        private readonly SapaFoRestRmsContext _context;
        private readonly IUserRepository _users;
        private readonly OtpService _otpService;
        private readonly IConfiguration _configuration;

        // mirror ReservationController limits
        private static readonly Dictionary<string, OtpInfo> _otpCache = new();

        private class OtpInfo
        {
            public string OtpCode { get; set; } = string.Empty;
            public DateTime Expired { get; set; }
            public int DailyCount { get; set; }
            public DateTime LastSent { get; set; }
            public List<DateTime> Timestamps { get; set; } = new();
        }

        public PhoneAuthService(SapaFoRestRmsContext context, IUserRepository users, OtpService otpService, IConfiguration configuration)
        {
            _context = context;
            _users = users;
            _otpService = otpService;
            _configuration = configuration;
        }

        public async Task RequestOtpAsync(string phone, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(phone)) throw new ArgumentException("Phone is required");

            var now = DateTime.Now;
            if (_otpCache.ContainsKey(phone))
            {
                var info = _otpCache[phone];
                if (info.LastSent.Date != now.Date)
                {
                    info.DailyCount = 0;
                    info.LastSent = now;
                    info.Timestamps.Clear();
                }

                info.Timestamps = info.Timestamps.Where(t => (now - t).TotalMinutes < 10).ToList();
                if (info.Timestamps.Count >= 2)
                    throw new InvalidOperationException("Bạn đã gửi OTP quá 2 lần trong 10 phút, vui lòng thử lại sau.");
                if (info.DailyCount >= 3)
                    throw new InvalidOperationException("Bạn đã gửi OTP quá 3 lần trong ngày, vui lòng thử lại vào ngày mai.");
            }

            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Phone == phone && u.IsDeleted == false, ct);
            if (user == null || user.RoleId != 5) // Customer only
            {
                throw new KeyNotFoundException("Không tìm thấy tài khoản khách hàng cho số điện thoại này");
            }

            var otp = new Random().Next(100000, 999999).ToString();
            var expired = now.AddMinutes(5);
            var sent = await _otpService.SendOtpAsync(phone, otp);
            if (!sent) throw new InvalidOperationException("Không thể gửi OTP, vui lòng thử lại.");

            if (!_otpCache.ContainsKey(phone))
            {
                _otpCache[phone] = new OtpInfo { OtpCode = otp, Expired = expired, DailyCount = 1, LastSent = now, Timestamps = new List<DateTime> { now } };
            }
            else
            {
                var info = _otpCache[phone];
                info.OtpCode = otp;
                info.Expired = expired;
                info.DailyCount++;
                info.LastSent = now;
                info.Timestamps.Add(now);
            }
        }

        public async Task<LoginResponse> VerifyOtpAsync(string phone, string code, CancellationToken ct = default)
        {
            if (!_otpCache.ContainsKey(phone)) throw new InvalidOperationException("Chưa gửi OTP đến số này.");
            var info = _otpCache[phone];
            if (DateTime.Now > info.Expired) throw new InvalidOperationException("Mã OTP đã hết hạn.");
            if (code != info.OtpCode) throw new InvalidOperationException("Mã OTP không chính xác.");

            var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Phone == phone && u.IsDeleted == false, ct);
            if (user == null || user.RoleId != 5) throw new UnauthorizedAccessException("Tài khoản không hợp lệ");

            _otpCache.Remove(phone);

            // Issue JWT
            var jwtSection = _configuration.GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["Key"] ?? "replace-with-strong-key"));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var claims = new[]
            {
                new Claim("userId", user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.FullName ?? user.Email ?? phone),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim(ClaimTypes.Role, user.Role?.RoleName ?? "Customer")
            };
            var descriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(2),
                Issuer = jwtSection["Issuer"],
                Audience = jwtSection["Audience"],
                SigningCredentials = creds
            };
            var handler = new JwtSecurityTokenHandler();
            var token = handler.CreateToken(descriptor);
            var jwt = handler.WriteToken(token);

            return new LoginResponse
            {
                UserId = user.UserId,
                FullName = user.FullName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                RoleId = user.RoleId,
                RoleName = user.Role?.RoleName ?? "Customer",
                Token = jwt
            };
        }
    }
}


