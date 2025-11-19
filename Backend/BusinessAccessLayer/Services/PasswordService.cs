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
      <h2 style='color:#2c3e50;margin-top:0;'>Đặt lại mật khẩu</h2>
      <p>Chào {user.FullName},</p>
      <p>Chúng tôi nhận được yêu cầu đặt lại mật khẩu cho tài khoản của bạn.</p>
      <div style='background-color:#f0f0f0;padding:15px;border-radius:5px;margin:20px 0;text-align:center;'>
        <p style='margin:0;font-size:18px;font-weight:bold;color:#2c3e50;letter-spacing:3px;'>{code}</p>
      </div>
      <p>Mã xác nhận này sẽ hết hạn sau <strong>10 phút</strong>.</p>
      <p style='color:#e74c3c;'><strong>Lưu ý:</strong> Nếu bạn không yêu cầu đặt lại mật khẩu, vui lòng bỏ qua email này.</p>
      <hr style='border:none;border-top:1px solid #eee;margin:30px 0;' />
      <p style='font-size:12px;color:#999;margin:0;'>Đây là email tự động, vui lòng không trả lời.</p>
      <p style='font-size:12px;color:#999;margin:5px 0 0 0;'>SapaFoRest RMS</p>
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
            if (user == null || user.IsDeleted == true) throw new UnauthorizedAccessException("Invalid request");
            var ok = await _verificationService.VerifyCodeAsync(user.UserId, "ResetPassword", request.Code, ct);
            if (!ok) throw new UnauthorizedAccessException("Invalid verification code");

            var newPassword = DomainAccessLayer.Common.PasswordGenerator.Generate();
            user.PasswordHash = HashPassword(newPassword);
            await _userRepository.UpdateAsync(user);
            await _context.SaveChangesAsync(ct);
            await _emailService.SendAsync(user.Email, "Your new password", $"Your new password is: {newPassword}");
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
<div style='font-family:Segoe UI,Helvetica,Arial,sans-serif;font-size:14px;'>
  <p>Chào {user.FullName},</p>
  <p>Mật khẩu của bạn đã được đặt lại thành công.</p>
  <p>Nếu bạn không thực hiện thao tác này, vui lòng liên hệ quản trị viên ngay lập tức.</p>
  <p>Trân trọng,</p>
  <p>SapaFoRest RMS</p>
  <hr />
  <small>Đây là email tự động, vui lòng không trả lời.</small>
</div>";
                await _emailService.SendAsync(user.Email, "Mật khẩu đã được đặt lại", emailBody);
            }
            catch
            {
                // Ignore email errors
            }
        }

        public async Task RequestChangeAsync(RequestChangePasswordDto request, CancellationToken ct = default)
        {
            var user = await _userRepository.GetByIdAsync(request.UserId);
            if (user == null || user.IsDeleted == true) throw new UnauthorizedAccessException("Invalid request");
            if (!VerifyPassword(request.CurrentPassword, user.PasswordHash)) throw new UnauthorizedAccessException("Invalid current password");
            await _verificationService.GenerateAndSendCodeAsync(user.UserId, user.Email, "ChangePassword", 10, ct);
        }

        public async Task ChangeAsync(VerifyChangePasswordDto request, CancellationToken ct = default)
        {
            var user = await _userRepository.GetByIdAsync(request.UserId);
            if (user == null || user.IsDeleted == true) throw new UnauthorizedAccessException("Invalid request");
            var ok = await _verificationService.VerifyCodeAsync(user.UserId, "ChangePassword", request.Code, ct);
            if (!ok) throw new UnauthorizedAccessException("Invalid verification code");
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


