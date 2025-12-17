using System.Text;
using System.Text.Json;
using WebSapaFoRestForCustomer.DTOs;
using WebSapaFoRestForCustomer.Models;

namespace WebSapaFoRestForCustomer.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ApiService(HttpClient httpClient, IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
        }

        private string GetApiBaseUrl()
        {
            return _configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7096";
        }

        private HttpClient GetAuthenticatedClient()
        {
            var client = new HttpClient();
            client.BaseAddress = new Uri(GetApiBaseUrl());

            // Get token from session first (refreshed token is stored in Session), then fallback to claims
            var token =
                _httpContextAccessor.HttpContext?.Session.GetString("Token")
                ?? _httpContextAccessor.HttpContext?.User?.FindFirst("Token")?.Value;
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }

            return client;
        }

        private async Task<bool> TryRefreshTokenAsync()
        {
            try
            {
                var refreshToken = _httpContextAccessor.HttpContext?.User?.FindFirst("RefreshToken")?.Value
                    ?? _httpContextAccessor.HttpContext?.Session.GetString("RefreshToken");
                if (string.IsNullOrEmpty(refreshToken)) return false;

                var payload = new { RefreshToken = refreshToken };
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{GetApiBaseUrl()}/api/Auth/refresh-token", content);
                if (!response.IsSuccessStatusCode) return false;

                var data = await response.Content.ReadAsStringAsync();
                var refreshed = JsonSerializer.Deserialize<LoginResponse>(data, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (refreshed == null || string.IsNullOrEmpty(refreshed.Token)) return false;

                // store new tokens
                _httpContextAccessor.HttpContext?.Session.SetString("Token", refreshed.Token);
                if (!string.IsNullOrEmpty(refreshed.RefreshToken))
                {
                    _httpContextAccessor.HttpContext?.Session.SetString("RefreshToken", refreshed.RefreshToken);
                }
                return true;
            }
            catch { return false; }
        }

        private async Task<HttpResponseMessage> SendWithAutoRefreshAsync(Func<HttpClient, Task<HttpResponseMessage>> send)
        {
            using var client = GetAuthenticatedClient();
            var response = await send(client);
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                if (await TryRefreshTokenAsync())
                {
                    using var client2 = GetAuthenticatedClient();
                    response = await send(client2);
                }
            }
            return response;
        }

        // Customer Authentication Methods
        public async Task<bool> SendOtpAsync(string phone)
        {
            try
            {
                var json = JsonSerializer.Serialize(phone);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{GetApiBaseUrl()}/api/Customer/send-otp-login", content);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<LoginResponse?> VerifyOtpAsync(string phone, string code)
        {
            try
            {
                var verifyDto = new { Phone = phone, Code = code };
                var json = JsonSerializer.Serialize(verifyDto);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{GetApiBaseUrl()}/api/Customer/verify-otp-login", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<LoginResponse>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Get customer profile
        /// </summary>
        public async Task<CustomerProfile?> GetCustomerProfileAsync()
        {
            try
            {
                var response = await SendWithAutoRefreshAsync(c => c.GetAsync("api/Customer/profile"));

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<CustomerProfile>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Update customer profile
        /// </summary>
        public async Task<CustomerProfile?> UpdateCustomerProfileAsync(CustomerProfileUpdate request)
        {
            try
            {
                // Create form data for file upload
                using var formData = new MultipartFormDataContent();

                // Add basic fields
                formData.Add(new StringContent(request.FullName), "FullName");
                if (!string.IsNullOrEmpty(request.Email))
                    formData.Add(new StringContent(request.Email), "Email");
                if (!string.IsNullOrEmpty(request.Phone))
                    formData.Add(new StringContent(request.Phone), "Phone");
                if (!string.IsNullOrEmpty(request.AvatarUrl))
                    formData.Add(new StringContent(request.AvatarUrl), "AvatarUrl");

                // Add file if provided
                if (request.AvatarFile != null && request.AvatarFile.Length > 0)
                {
                    var fileContent = new StreamContent(request.AvatarFile.OpenReadStream());
                    fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(request.AvatarFile.ContentType);
                    formData.Add(fileContent, "AvatarFile", request.AvatarFile.FileName);
                }

                var response = await SendWithAutoRefreshAsync(c => c.PutAsync("api/Customer/profile", formData));

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    // Backend returns updated profile as JSON
                    var updated = JsonSerializer.Deserialize<CustomerProfile>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    return updated;
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        // ====================== CHANGE EMAIL/PHONE (OTP) ======================
        public async Task<bool> SendChangeEmailOtpAsync(string newEmail)
        {
            try
            {
                var payload = new { Email = newEmail };
                var json = JsonSerializer.Serialize(payload);
                using var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await SendWithAutoRefreshAsync(c => c.PostAsync("api/Customer/profile/change-email/send-otp", content));
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        public async Task<(bool Success, string? Message)> VerifyChangeEmailOtpAsync(string newEmail, string code)
        {
            try
            {
                var payload = new { Email = newEmail, Code = code };
                var json = JsonSerializer.Serialize(payload);
                using var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await SendWithAutoRefreshAsync(c => c.PostAsync("api/Customer/profile/change-email/verify", content));
                var body = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode) return (true, null);

                try
                {
                    using var doc = JsonDocument.Parse(body);
                    if (doc.RootElement.TryGetProperty("message", out var msg)) return (false, msg.GetString());
                }
                catch { }

                return (false, "Xác thực OTP email thất bại.");
            }
            catch { return (false, "Xác thực OTP email thất bại."); }
        }

        public async Task<bool> SendChangePhoneOtpAsync(string newPhone)
        {
            try
            {
                var payload = new { Phone = newPhone };
                var json = JsonSerializer.Serialize(payload);
                using var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await SendWithAutoRefreshAsync(c => c.PostAsync("api/Customer/profile/change-phone/send-otp", content));
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        public async Task<(bool Success, string? Message)> VerifyChangePhoneOtpAsync(string newPhone, string code)
        {
            try
            {
                var payload = new { Phone = newPhone, Code = code };
                var json = JsonSerializer.Serialize(payload);
                using var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await SendWithAutoRefreshAsync(c => c.PostAsync("api/Customer/profile/change-phone/verify", content));
                var body = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode) return (true, null);

                try
                {
                    using var doc = JsonDocument.Parse(body);
                    if (doc.RootElement.TryGetProperty("message", out var msg)) return (false, msg.GetString());
                }
                catch { }

                return (false, "Xác thực OTP số điện thoại thất bại.");
            }
            catch { return (false, "Xác thực OTP số điện thoại thất bại."); }
        }

        // Generic API response wrapper
        public class ApiResponse<T>
        {
            public bool Success { get; set; }
            public string? Message { get; set; }
            public T? Data { get; set; }
            public List<string>? Errors { get; set; }
        }
    }
}
