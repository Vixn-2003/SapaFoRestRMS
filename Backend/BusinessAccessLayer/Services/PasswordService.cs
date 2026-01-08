using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BusinessAccessLayer.DTOs.Auth;
using BusinessAccessLayer.Services.Interfaces;
using DataAccessLayer.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using DomainAccessLayer.Models;
using DataAccessLayer.Dbcontext;

namespace BusinessAccessLayer.Services
{
    public class PasswordService : IPasswordService
    {
        private readonly IUserRepository _userRepository;
        private readonly IVerificationService _verificationService;
        private readonly IEmailService _emailService;
        private readonly SapaFoRestRmsContext _context;

        public PasswordService(IUserRepository userRepository, IVerificationService verificationService, IEmailService emailService, SapaFoRestRmsContext context)
        {
            _userRepository = userRepository;
            _verificationService = verificationService;
            _emailService = emailService;
            _context = context;
        }

        public async Task RequestResetAsync(RequestPasswordResetDto request, CancellationToken ct = default)
        {
            var user = await _userRepository.GetByEmailAsync(request.Email);
            if (user == null || user.IsDeleted == true) return; // don't reveal
            
            // Generate code
            var code = await _verificationService.GenerateAndSendCodeAsync(user.UserId, user.Email, "ResetPassword", 10, ct);
            
            // Send custom email with better template
            try
            {
                var emailBody = $@"
<div style='font-family:Segoe UI,Helvetica,Arial,sans-serif;font-size:14px;line-height:1.6;color:#333;'>
  <div style='max-width:600px;margin:0 auto;padding:20px;background-color:#f9f9f9;'>
    <div style='background-color:#fff;padding:30px;border-radius:8px;box-shadow:0 2px 4px rgba(0,0,0,0.1);'>

      <div style='text-align:center;margin-bottom:30px;'>
        <h1 style='color:#2c3e50;margin:0;font-size:24px;'>SapaFoRest RMS</h1>
        <p style='color:#7f8c8d;margin:5px 0;font-size:14px;'>Hệ thống quản lý nhà hàng</p>
      </div>

      <h2 style='color:#2c3e50;margin-top:0;text-align:center;'>Yêu Cầu Đặt Lại Mật Khẩu</h2>

      <p>Xin chào <strong>{user.FullName}</strong>,</p>

      <p>Chúng tôi nhận được yêu cầu đặt lại mật khẩu cho tài khoản <strong>SapaFoRest RMS</strong> của bạn.</p>

      <p>Để hoàn tất quá trình đặt lại mật khẩu, vui lòng sử dụng mã xác nhận dưới đây:</p>

      <div style='background-color:#f8f9fa;padding:25px;border-radius:8px;margin:25px 0;text-align:center;border:2px solid #3498db;'>
        <p style='margin:0 0 10px 0;font-size:16px;color:#2c3e50;'><strong>Mã xác nhận:</strong></p>
        <p style='margin:0;font-size:28px;font-weight:bold;color:#e74c3c;letter-spacing:4px;font-family:monospace;'>{code}</p>
      </div>

      <div style='background-color:#d4edda;padding:15px;border-radius:5px;margin:20px 0;border-left:4px solid #28a745;'>
        <p style='margin:0;color:#155724;'><strong>⏰ Thời hạn:</strong> Mã xác nhận này sẽ hết hạn sau <strong>10 phút</strong></p>
      </div>

      <div style='background-color:#fff3cd;padding:15px;border-radius:5px;margin:20px 0;border-left:4px solid #f39c12;'>
        <p style='margin:0;color:#856404;'><strong>⚠️ Lưu ý bảo mật:</strong></p>
        <ul style='margin:10px 0;padding-left:20px;color:#856404;'>
          <li>Mã xác nhận chỉ có thể sử dụng một lần</li>
          <li>Không chia sẻ mã này với bất kỳ ai</li>
          <li>Nếu bạn không yêu cầu đặt lại mật khẩu, vui lòng bỏ qua email này</li>
        </ul>
      </div>

      <p>Nếu bạn gặp khó khăn trong quá trình đặt lại mật khẩu, vui lòng liên hệ bộ phận hỗ trợ kỹ thuật.</p>

      <p style='text-align:center;margin:30px 0;'>
        <a href='#' style='background-color:#3498db;color:#fff;padding:12px 30px;text-decoration:none;border-radius:5px;font-weight:bold;display:inline-block;'>Đặt Lại Mật Khẩu</a>
      </p>

      <hr style='border:none;border-top:1px solid #eee;margin:30px 0;' />

      <div style='text-align:center;'>
        <p style='font-size:12px;color:#999;margin:0;'>Đây là email tự động từ hệ thống SapaFoRest RMS</p>
        <p style='font-size:12px;color:#999;margin:5px 0 0 0;'>Vui lòng không trả lời email này</p>
        <p style='font-size:12px;color:#999;margin:5px 0 0 0;'>© 2024 SapaFoRest RMS. Tất cả quyền được bảo lưu.</p>
      </div>
    </div>
  </div>
</div>";
                await _emailService.SendAsync(user.Email, "Mã xác nhận đặt lại mật khẩu - SapaFoRest RMS", emailBody);
            }
            catch
            {
                // VerificationService already sent a basic email, so we can ignore this
            }
        }

        public async Task<string> VerifyResetAsync(VerifyPasswordResetDto request, CancellationToken ct = default)
        {
            var user = await _userRepository.GetByEmailAsync(request.Email);
            if (user == null || user.IsDeleted == true) throw new UnauthorizedAccessException("Yêu cầu không hợp lệ");
            var ok = await _verificationService.VerifyCodeAsync(user.UserId, "ResetPassword", request.Code, ct);
            if (!ok) throw new UnauthorizedAccessException("Mã xác nhận không hợp lệ");

            var newPassword = DomainAccessLayer.Common.PasswordGenerator.Generate();
            user.PasswordHash = HashPassword(newPassword);
            await _userRepository.UpdateAsync(user);
            await _context.SaveChangesAsync(ct);
            var newPasswordEmailBody = $@"
<div style='font-family:Segoe UI,Helvetica,Arial,sans-serif;font-size:14px;line-height:1.6;color:#333;'>
  <div style='max-width:600px;margin:0 auto;padding:20px;background-color:#f9f9f9;'>
    <div style='background-color:#fff;padding:30px;border-radius:8px;box-shadow:0 2px 4px rgba(0,0,0,0.1);'>
      <div style='text-align:center;margin-bottom:30px;'>
        <h1 style='color:#2c3e50;margin:0;font-size:24px;'>SapaFoRest RMS</h1>
        <p style='color:#7f8c8d;margin:5px 0;font-size:14px;'>Hệ thống quản lý nhà hàng</p>
      </div>

      <h2 style='color:#2c3e50;margin-top:0;text-align:center;'>Mật Khẩu Mới Của Bạn</h2>

      <p>Xin chào <strong>{user.FullName}</strong>,</p>

      <p>Mật khẩu của bạn đã được đặt lại thành công theo yêu cầu khôi phục.</p>

      <div style='background-color:#f8f9fa;padding:20px;border-radius:8px;margin:25px 0;border-left:4px solid:#3498db;text-align:center;'>
        <p style='margin:0;font-size:16px;color:#2c3e50;'><strong>Mật khẩu mới:</strong></p>
        <p style='margin:10px 0;font-size:20px;font-weight:bold;color:#e74c3c;letter-spacing:2px;'>{newPassword}</p>
      </div>

      <div style='background-color:#fff3cd;padding:15px;border-radius:5px;margin:20px 0;border-left:4px solid:#f39c12;'>
        <p style='margin:0;color:#856404;'><strong>⚠️ Lưu ý bảo mật:</strong></p>
        <ul style='margin:10px 0;padding-left:20px;color:#856404;'>
          <li>Vui lòng đổi mật khẩu này ngay sau khi đăng nhập</li>
          <li>Không chia sẻ mật khẩu với người khác</li>
          <li>Liên hệ quản trị viên nếu bạn không yêu cầu đặt lại mật khẩu</li>
        </ul>
      </div>

      <p style='text-align:center;margin:30px 0;'>
        <a href='#' style='background-color:#3498db;color:#fff;padding:12px 30px;text-decoration:none;border-radius:5px;font-weight:bold;display:inline-block;'>Đăng Nhập Ngay</a>
      </p>

      <hr style='border:none;border-top:1px solid #eee;margin:30px 0;' />

      <div style='text-align:center;'>
        <p style='font-size:12px;color:#999;margin:0;'>Đây là email tự động từ hệ thống SapaFoRest RMS</p>
        <p style='font-size:12px;color:#999;margin:5px 0 0 0;'>Vui lòng không trả lời email này</p>
        <p style='font-size:12px;color:#999;margin:5px 0 0 0;'>© 2024 SapaFoRest RMS. Tất cả quyền được bảo lưu.</p>
      </div>
    </div>
  </div>
</div>";
            await _emailService.SendAsync(user.Email, "Mật khẩu mới - SapaFoRest RMS", newPasswordEmailBody);
            return newPassword;
        }

        public async Task ResetPasswordAsync(ResetPasswordDto request, CancellationToken ct = default)
        {
            var user = await _userRepository.GetByEmailAsync(request.Email);
            if (user == null || user.IsDeleted == true) 
                throw new UnauthorizedAccessException("Email không tồn tại hoặc tài khoản đã bị xóa");

            // Verify code
            var ok = await _verificationService.VerifyCodeAsync(user.UserId, "ResetPassword", request.Code, ct);
            if (!ok) 
                throw new UnauthorizedAccessException("Mã xác nhận không hợp lệ hoặc đã hết hạn");

            // Validate password
            if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 8)
                throw new ArgumentException("Mật khẩu phải có ít nhất 8 ký tự");

            if (request.NewPassword != request.ConfirmPassword)
                throw new ArgumentException("Mật khẩu xác nhận không khớp");

            // Update password
            user.PasswordHash = HashPassword(request.NewPassword);
            user.ModifiedAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);
            await _context.SaveChangesAsync(ct);

            // Send confirmation email
            try
            {
                var emailBody = $@"
<div style='font-family:Segoe UI,Helvetica,Arial,sans-serif;font-size:14px;line-height:1.6;color:#333;'>
  <div style='max-width:600px;margin:0 auto;padding:20px;background-color:#f9f9f9;'>
    <div style='background-color:#fff;padding:30px;border-radius:8px;box-shadow:0 2px 4px rgba(0,0,0,0.1);'>

      <div style='text-align:center;margin-bottom:30px;'>
        <h1 style='color:#2c3e50;margin:0;font-size:24px;'>SapaFoRest RMS</h1>
        <p style='color:#7f8c8d;margin:5px 0;font-size:14px;'>Hệ thống quản lý nhà hàng</p>
      </div>

      <div style='text-align:center;margin-bottom:30px;'>
        <div style='width:80px;height:80px;background-color:#28a745;border-radius:50%;display:inline-flex;align-items:center;justify-content:center;margin:0 auto 15px;'>
          <span style='color:#fff;font-size:36px;'>✓</span>
        </div>
        <h2 style='color:#28a745;margin:0;'>Đặt Lại Mật Khẩu Thành Công</h2>
      </div>

      <p>Xin chào <strong>{user.FullName}</strong>,</p>

      <p>Chúng tôi xin thông báo rằng mật khẩu tài khoản <strong>SapaFoRest RMS</strong> của bạn đã được đặt lại thành công.</p>

      <div style='background-color:#d4edda;padding:20px;border-radius:8px;margin:25px 0;border-left:4px solid #28a745;'>
        <p style='margin:0;color:#155724;'><strong>✅ Hoàn tất:</strong> Mật khẩu mới đã được cập nhật vào hệ thống</p>
        <p style='margin:10px 0 0 0;color:#155724;'><strong>🕒 Thời gian:</strong> {DateTime.UtcNow.AddHours(7):dd/MM/yyyy HH:mm:ss} (GMT+7)</p>
      </div>

      <div style='background-color:#d1ecf1;padding:15px;border-radius:5px;margin:20px 0;border-left:4px solid #17a2b8;'>
        <p style='margin:0;color:#0c5460;'><strong>💡 Lời khuyên bảo mật:</strong></p>
        <ul style='margin:10px 0;padding-left:20px;color:#0c5460;'>
          <li>Sử dụng mật khẩu mạnh với ít nhất 8 ký tự</li>
          <li>Kết hợp chữ cái, số và ký tự đặc biệt</li>
          <li>Không sử dụng thông tin cá nhân dễ đoán</li>
          <li>Thay đổi mật khẩu định kỳ để đảm bảo an toàn</li>
        </ul>
      </div>

      <div style='background-color:#fff3cd;padding:15px;border-radius:5px;margin:20px 0;border-left:4px solid #f39c12;'>
        <p style='margin:0;color:#856404;'><strong>⚠️ Cảnh báo bảo mật:</strong> Nếu bạn không thực hiện thao tác đặt lại mật khẩu này, vui lòng:</p>
        <ul style='margin:10px 0;padding-left:20px;color:#856404;'>
          <li>Liên hệ quản trị viên hệ thống ngay lập tức</li>
          <li>Kiểm tra nhật ký hoạt động tài khoản</li>
          <li>Thay đổi mật khẩu nếu nghi ngờ bị xâm phạm</li>
        </ul>
      </div>

      <p>Nếu bạn cần hỗ trợ thêm, vui lòng liên hệ bộ phận kỹ thuật hoặc quản trị viên hệ thống.</p>

      <p style='text-align:center;margin:30px 0;'>
        <a href='#' style='background-color:#3498db;color:#fff;padding:12px 30px;text-decoration:none;border-radius:5px;font-weight:bold;display:inline-block;'>Đăng Nhập Ngay</a>
      </p>

      <hr style='border:none;border-top:1px solid #eee;margin:30px 0;' />

      <div style='text-align:center;'>
        <p style='font-size:12px;color:#999;margin:0;'>Đây là email tự động từ hệ thống SapaFoRest RMS</p>
        <p style='font-size:12px;color:#999;margin:5px 0 0 0;'>Vui lòng không trả lời email này</p>
        <p style='font-size:12px;color:#999;margin:5px 0 0 0;'>© 2024 SapaFoRest RMS. Tất cả quyền được bảo lưu.</p>
      </div>
    </div>
  </div>
</div>";
                await _emailService.SendAsync(user.Email, "Xác nhận đặt lại mật khẩu - SapaFoRest RMS", emailBody);
            }
            catch
            {
                // Ignore email errors
            }
        }

        public async Task RequestChangeAsync(RequestChangePasswordDto request, CancellationToken ct = default)
        {
            var user = await _userRepository.GetByIdAsync(request.UserId);
            if (user == null || user.IsDeleted == true) throw new UnauthorizedAccessException("Yêu cầu không hợp lệ");
            if (!VerifyPassword(request.CurrentPassword, user.PasswordHash)) throw new UnauthorizedAccessException("Mật khẩu hiện tại không đúng");
            await _verificationService.GenerateAndSendCodeAsync(user.UserId, user.Email, "ChangePassword", 10, ct);
        }

        public async Task ChangeAsync(VerifyChangePasswordDto request, CancellationToken ct = default)
        {
            var user = await _userRepository.GetByIdAsync(request.UserId);
            if (user == null || user.IsDeleted == true) throw new UnauthorizedAccessException("Yêu cầu không hợp lệ");
            var ok = await _verificationService.VerifyCodeAsync(user.UserId, "ChangePassword", request.Code, ct);
            if (!ok) throw new UnauthorizedAccessException("Mã xác nhận không hợp lệ");
            user.PasswordHash = HashPassword(request.NewPassword);
            user.ModifiedAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);
            await _context.SaveChangesAsync(ct);
        }

        private static string HashPassword(string password)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }

        private static bool VerifyPassword(string password, string hash)
        {
            return HashPassword(password) == hash;
        }
    }
}


