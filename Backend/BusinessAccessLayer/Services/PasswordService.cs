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
            await _verificationService.GenerateAndSendCodeAsync(user.UserId, user.Email, "ResetPassword", 10, ct);
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


