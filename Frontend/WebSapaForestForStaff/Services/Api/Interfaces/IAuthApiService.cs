using WebSapaForestForStaff.DTOs.Auth;
using WebSapaForestForStaff.DTOs.UserManagement;
using WebSapaForestForStaff.Services.Api;
using static WebSapaForestForStaff.Services.ApiService;

namespace WebSapaForestForStaff.Services.Api.Interfaces
{
    /// <summary>
    /// Interface for authentication-related API operations
    /// </summary>
    public interface IAuthApiService : IBaseApiService
    {
        /// <summary>
        /// Authenticates a user and returns login response with token
        /// </summary>
        Task<LoginResponse?> LoginAsync(LoginRequest request);

        /// <summary>
        /// Logs out the current user by clearing tokens
        /// </summary>
        void Logout();

        /// <summary>
        /// Sends forgot password email to the specified email address
        /// </summary>
        Task<bool> ForgotPasswordAsync(string email);

        /// <summary>
        /// Resets password using reset token and new password
        /// </summary>
        Task<bool> ResetPasswordAsync(ResetPasswordRequest request);

        /// <summary>
        /// Requests a password change by sending verification code to user's email
        /// </summary>
        Task<ApiResult> RequestPasswordChangeAsync(string currentPassword);

        /// <summary>
        /// Confirms password change using verification code and new password
        /// </summary>
        Task<ApiResult> ConfirmPasswordChangeAsync(string code, string newPassword);

        /// <summary>
        /// Creates a new manager account (Admin only)
        /// </summary>
        Task<bool> CreateManagerAsync(CreateManagerRequest request);

        /// <summary>
        /// Creates a new staff account (Manager only)
        /// </summary>
        Task<ApiResult> CreateStaffAsync(CreateStaffRequest request);

        /// <summary>
        /// Sends verification code to staff email before creation
        /// </summary>
        Task<ApiResult> SendStaffVerificationCodeAsync(CreateStaffVerificationRequest request);
    }
}

