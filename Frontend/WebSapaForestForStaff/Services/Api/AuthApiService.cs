using System.Text;
using System.Text.Json;
using WebSapaForestForStaff.DTOs.Auth;
using WebSapaForestForStaff.DTOs.UserManagement;
using WebSapaForestForStaff.Services.Api.Interfaces;
using static WebSapaForestForStaff.Services.ApiService;

namespace WebSapaForestForStaff.Services.Api
{
    /// <summary>
    /// Service for authentication-related API operations
    /// </summary>
    public class AuthApiService : BaseApiService, IAuthApiService
    {
        public AuthApiService(HttpClient httpClient, IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
            : base(httpClient, configuration, httpContextAccessor)
        {
        }

        /// <summary>
        /// Authenticates a user and returns login response with token
        /// </summary>
        public async Task<LoginResponse?> LoginAsync(LoginRequest request)
        {
            try
            {
                var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = null,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{GetApiBaseUrl()}/Auth/login", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var loginResponse = JsonSerializer.Deserialize<LoginResponse>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (loginResponse != null)
                    {
                        SetToken(loginResponse.Token);
                        if (!string.IsNullOrEmpty(loginResponse.RefreshToken))
                        {
                            _httpContextAccessor.HttpContext?.Session.SetString("RefreshToken", loginResponse.RefreshToken);
                        }
                    }

                    return loginResponse;
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Logs out the current user by clearing tokens
        /// </summary>
        public void Logout()
        {
            ClearToken();
        }

        /// <summary>
        /// Sends forgot password email to the specified email address
        /// </summary>
        public async Task<bool> ForgotPasswordAsync(string email)
        {
            try
            {
                var request = new { Email = email };
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{GetApiBaseUrl()}/Auth/forgot-password", content);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Resets password using reset token and new password
        /// </summary>
        public async Task<bool> ResetPasswordAsync(ResetPasswordRequest request)
        {
            try
            {
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{GetApiBaseUrl()}/Auth/reset-password", content);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Requests a password change by sending verification code to user's email
        /// </summary>
        public async Task<ApiService.ApiResult> RequestPasswordChangeAsync(string currentPassword)
        {
            if (string.IsNullOrWhiteSpace(currentPassword))
            {
                return new ApiService.ApiResult(false, "Vui lòng nhập mật khẩu hiện tại.");
            }

            try
            {
                var payload = new
                {
                    UserId = 0,
                    CurrentPassword = currentPassword
                };
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await SendWithAutoRefreshAsync(c => c.PostAsync($"{GetApiBaseUrl()}/password/change/request", content));
                if (response.IsSuccessStatusCode)
                {
                    var message = await ReadApiMessageAsync(response) ?? "Mã xác nhận đã được gửi tới email của bạn.";
                    return new ApiService.ApiResult(true, message);
                }

                var error = await ReadApiMessageAsync(response) ?? "Không thể gửi mã xác nhận. Vui lòng thử lại.";
                return new ApiService.ApiResult(false, error);
            }
            catch
            {
                return new ApiService.ApiResult(false, "Không thể kết nối tới máy chủ. Vui lòng thử lại sau.");
            }
        }

        /// <summary>
        /// Confirms password change using verification code and new password
        /// </summary>
        public async Task<ApiService.ApiResult> ConfirmPasswordChangeAsync(string code, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(newPassword))
            {
                return new ApiService.ApiResult(false, "Vui lòng nhập mã xác nhận và mật khẩu mới.");
            }

            try
            {
                var payload = new
                {
                    UserId = 0,
                    Code = code,
                    NewPassword = newPassword
                };
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await SendWithAutoRefreshAsync(c => c.PostAsync($"{GetApiBaseUrl()}/password/change/confirm", content));
                if (response.IsSuccessStatusCode)
                {
                    var message = await ReadApiMessageAsync(response) ?? "Đổi mật khẩu thành công.";
                    return new ApiService.ApiResult(true, message);
                }

                var error = await ReadApiMessageAsync(response) ?? "Không thể đổi mật khẩu. Vui lòng kiểm tra lại thông tin.";
                return new ApiService.ApiResult(false, error);
            }
            catch
            {
                return new ApiService.ApiResult(false, "Không thể kết nối tới máy chủ. Vui lòng thử lại sau.");
            }
        }

        /// <summary>
        /// Creates a new manager account (Admin only)
        /// </summary>
        public async Task<bool> CreateManagerAsync(CreateManagerRequest request)
        {
            try
            {
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await SendWithAutoRefreshAsync(c => c.PostAsync($"{GetApiBaseUrl()}/auth/admin/create-manager", content));
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Creates a new staff account (Manager only)
        /// </summary>
        public async Task<ApiService.ApiResult> CreateStaffAsync(CreateStaffRequest request)
        {
            try
            {
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await SendWithAutoRefreshAsync(c => c.PostAsync($"{GetApiBaseUrl()}/auth/manager/create-staff", content));
                if (response.IsSuccessStatusCode)
                {
                    return new ApiService.ApiResult(true, "Tạo nhân viên thành công");
                }

                var error = await ReadApiMessageAsync(response) ?? "Không thể tạo nhân viên. Vui lòng kiểm tra lại thông tin.";
                return new ApiService.ApiResult(false, error);
            }
            catch
            {
                return new ApiService.ApiResult(false, "Không thể kết nối để tạo nhân viên.");
            }
        }

        public async Task<ApiService.ApiResult> SendStaffVerificationCodeAsync(CreateStaffVerificationRequest request)
        {
            try
            {
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await SendWithAutoRefreshAsync(c => c.PostAsync($"{GetApiBaseUrl()}/auth/manager/create-staff/send-code", content));
                if (response.IsSuccessStatusCode)
                {
                    return new ApiService.ApiResult(true, await ReadApiMessageAsync(response) ?? "Đã gửi mã xác minh.");
                }

                var message = await ReadApiMessageAsync(response) ?? "Không thể gửi mã xác minh. Vui lòng thử lại.";
                return new ApiService.ApiResult(false, message);
            }
            catch
            {
                return new ApiService.ApiResult(false, "Không thể kết nối để gửi mã xác minh.");
            }
        }
    }
}

