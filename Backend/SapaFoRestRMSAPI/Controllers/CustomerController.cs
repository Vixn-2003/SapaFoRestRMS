using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DataAccessLayer.Dbcontext;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Configuration;
namespace SapaFoRestRMSAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Customer")] 
    public class CustomerController : ControllerBase
    {
        private readonly SapaFoRestRmsContext _context;
        private readonly IConfiguration _configuration;
        private static Dictionary<string, OtpInfo> _otpCache = new();

        public CustomerController(SapaFoRestRmsContext context, Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public class OtpInfo
        {
            public string OtpCode { get; set; } = string.Empty;
            public DateTime Expired { get; set; }
            public int DailyCount { get; set; }
            public DateTime LastSent { get; set; }
            public List<DateTime> Timestamps { get; set; } = new();
        }

        // Anonymous: request OTP for phone login (mirrors ReservationController limits)
        [HttpPost("send-otp-login")]
        [AllowAnonymous]
        public async Task<IActionResult> SendOtpLogin([FromBody] string phone)
        {
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
                    return BadRequest(new { message = "Bạn đã gửi OTP quá 2 lần trong 10 phút, vui lòng thử lại sau." });
                if (info.DailyCount >= 3)
                    return BadRequest(new { message = "Bạn đã gửi OTP quá 3 lần trong ngày, vui lòng thử lại vào ngày mai." });
            }

            // Must be an existing Customer account bound to this phone
            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Phone == phone && u.IsDeleted == false);
            if (user == null || user.RoleId != 5)
            {
                return NotFound(new { message = "Không tìm thấy tài khoản khách hàng cho số điện thoại này" });
            }

            var otp = new Random().Next(100000, 999999).ToString();
            var expired = now.AddMinutes(5);

            var otpService = new BusinessAccessLayer.Services.OtpService();
            var sent = await otpService.SendOtpAsync(phone, otp);
            if (!sent)
                return BadRequest(new { message = "Không thể gửi OTP, vui lòng thử lại." });

            if (!_otpCache.ContainsKey(phone))
            {
                _otpCache[phone] = new OtpInfo
                {
                    OtpCode = otp,
                    Expired = expired,
                    DailyCount = 1,
                    LastSent = now,
                    Timestamps = new List<DateTime> { now }
                };
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

            Console.WriteLine($"[DEBUG OTP LOGIN] {phone}: {otp}");
            return Ok(new { message = "OTP đã được gửi.", expireAt = expired });
        }

        public class VerifyLoginDto { public string Phone { get; set; } = string.Empty; public string Code { get; set; } = string.Empty; }

        // Anonymous: verify OTP and return JWT for Customer
        [HttpPost("verify-otp-login")]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyOtpLogin([FromBody] VerifyLoginDto dto, CancellationToken ct)
        {
            if (!_otpCache.ContainsKey(dto.Phone))
                return BadRequest(new { message = "Chưa gửi OTP đến số này." });

            var info = _otpCache[dto.Phone];
            if (DateTime.Now > info.Expired)
                return BadRequest(new { message = "Mã OTP đã hết hạn." });
            if (dto.Code != info.OtpCode)
                return BadRequest(new { message = "Mã OTP không chính xác." });

            var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Phone == dto.Phone && u.IsDeleted == false, ct);
            if (user == null || user.RoleId != 5)
                return Unauthorized(new { message = "Tài khoản không hợp lệ" });

            _otpCache.Remove(dto.Phone);

            // Issue JWT token for Customer
            var jwtSection = _configuration.GetSection("Jwt");
            var key = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(jwtSection["Key"] ?? "replace-with-strong-key"));
            var creds = new Microsoft.IdentityModel.Tokens.SigningCredentials(key, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);
            var claims = new[]
            {
                new System.Security.Claims.Claim("userId", user.UserId.ToString()),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, user.FullName ?? user.Email),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Email, user.Email ?? string.Empty),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, user.Role?.RoleName ?? "Customer")
            };
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new System.Security.Claims.ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(2),
                Issuer = jwtSection["Issuer"],
                Audience = jwtSection["Audience"],
                SigningCredentials = creds
            };
            var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var jwt = tokenHandler.WriteToken(token);

            return Ok(new
            {
                userId = user.UserId,
                fullName = user.FullName,
                email = user.Email,
                roleId = user.RoleId,
                roleName = user.Role?.RoleName ?? "Customer",
                token = jwt
            });
        }

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile(CancellationToken ct)
        {
            var userIdClaim = User.FindFirst("userId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();
            var userId = int.Parse(userIdClaim);

            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId && u.IsDeleted == false, ct);
            if (user == null) return NotFound();

            var customer = await _context.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.UserId == userId, ct);

            return Ok(new
            {
                user.UserId,
                user.FullName,
                user.Email,
                user.Phone,
                user.Status,
                CustomerId = customer?.CustomerId,
                LoyaltyPoints = customer?.LoyaltyPoints ?? 0,
                Notes = customer?.Notes
            });
        }

        public class UpdateProfileDto
        {
            public string FullName { get; set; } = string.Empty;
            public string? Phone { get; set; }
            public string? Notes { get; set; }
        }

        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto, CancellationToken ct)
        {
            var userIdClaim = User.FindFirst("userId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();
            var userId = int.Parse(userIdClaim);

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId && u.IsDeleted == false, ct);
            if (user == null) return NotFound();

            user.FullName = dto.FullName;
            if (!string.IsNullOrWhiteSpace(dto.Phone)) user.Phone = dto.Phone;

            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == userId, ct);
            if (customer == null)
            {
                customer = new DomainAccessLayer.Models.Customer { UserId = userId, Notes = dto.Notes };
                await _context.Customers.AddAsync(customer, ct);
            }
            else
            {
                customer.Notes = dto.Notes;
                _context.Customers.Update(customer);
            }

            _context.Users.Update(user);
            await _context.SaveChangesAsync(ct);
            return NoContent();
        }

        [HttpGet("orders")]
        public async Task<IActionResult> GetOrders([FromQuery] string? status, CancellationToken ct)
        {
            var userIdClaim = User.FindFirst("userId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();
            var userId = int.Parse(userIdClaim);

            var customer = await _context.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.UserId == userId, ct);
            if (customer == null) return Ok(new object[] { });

            var query = _context.Orders.AsNoTracking().Where(o => o.CustomerId == customer.CustomerId);
            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(o => o.Status == status);
            }

            var items = await query.OrderByDescending(o => o.CreatedAt).ToListAsync(ct);
            return Ok(items);
        }
    }
}


