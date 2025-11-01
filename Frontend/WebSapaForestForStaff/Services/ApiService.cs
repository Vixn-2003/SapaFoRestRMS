using System.Text;
using System.Text.Json;
using WebSapaForestForStaff.DTOs;
using WebSapaForestForStaff.DTOs.Auth;
using WebSapaForestForStaff.DTOs.UserManagement;

namespace WebSapaForestForStaff.Services
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
            return _configuration["ApiSettings:BaseUrl"];
        }

        private string? GetToken()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null) return null;

            // First try to get from Session (for backward compatibility with ApiService.LoginAsync)
            var tokenFromSession = httpContext.Session.GetString("Token");
            if (!string.IsNullOrEmpty(tokenFromSession))
            {
                return tokenFromSession;
            }

            // If not in Session, try to get from Claims (where AuthController stores it)
            var tokenFromClaims = httpContext.User?.FindFirst("Token")?.Value;
            return tokenFromClaims;
        }

        private void SetToken(string token)
        {
            _httpContextAccessor.HttpContext?.Session.SetString("Token", token);
        }

        private void ClearToken()
        {
            _httpContextAccessor.HttpContext?.Session.Remove("Token");
        }

        private HttpClient GetAuthenticatedClient()
        {
            var token = GetToken();
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
            return _httpClient;
        }

        // Auth methods
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

        public void Logout()
        {
            ClearToken();
        }

        // User management methods
        public async Task<bool> CreateManagerAsync(CreateManagerRequest request)
        {
            try
            {
                using var client = GetAuthenticatedClient();
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync($"{GetApiBaseUrl()}/auth/admin/create-manager", content);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> CreateStaffAsync(CreateStaffRequest request)
        {
            try
            {
                using var client = GetAuthenticatedClient();
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync($"{GetApiBaseUrl()}/auth/manager/create-staff", content);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<User>?> GetUsersAsync()
        {
            try
            {
                using var client = GetAuthenticatedClient();
                var response = await client.GetAsync($"{GetApiBaseUrl()}/Users");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<List<User>>(content, new JsonSerializerOptions
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

        public async Task<int> GetTotalUsersAsync()
        {
            try
            {
                using var client = GetAuthenticatedClient();
                var response = await client.GetAsync($"{GetApiBaseUrl()}/users");
                if (!response.IsSuccessStatusCode) return 0;
                var content = await response.Content.ReadAsStringAsync();
                var users = JsonSerializer.Deserialize<List<User>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return users?.Count ?? 0;
            }
            catch
            {
                return 0;
            }
        }

        public async Task<int> GetTotalEventsAsync()
        {
            try
            {
                using var client = GetAuthenticatedClient();
                var response = await client.GetAsync($"{GetApiBaseUrl()}/events");
                if (!response.IsSuccessStatusCode) return 0;
                var content = await response.Content.ReadAsStringAsync();
                var events = JsonSerializer.Deserialize<List<object>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return events?.Count ?? 0;
            }
            catch
            {
                return 0;
            }
        }

        public async Task<int> GetTotalPendingOrConfirmedReservationsAsync()
        {
            try
            {
                using var client = GetAuthenticatedClient();
                var response = await client.GetAsync($"{GetApiBaseUrl()}/reservationstaff/reservations/pending-confirmed?page=1&pageSize=1");
                if (!response.IsSuccessStatusCode) return 0;
                var content = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(content);
                if (doc.RootElement.TryGetProperty("TotalCount", out var total))
                {
                    return total.GetInt32();
                }
                return 0;
            }
            catch
            {
                return 0;
            }
        }

        public async Task<User?> GetUserAsync(int id)
        {
            try
            {
                using var client = GetAuthenticatedClient();
                var response = await client.GetAsync($"{GetApiBaseUrl()}/users/{id}");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<User>(content, new JsonSerializerOptions
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

        public async Task<bool> UpdateUserAsync(User user)
        {
            try
            {
                using var client = GetAuthenticatedClient();
                var json = JsonSerializer.Serialize(user);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PutAsync($"{GetApiBaseUrl()}/users/{user.UserId}", content);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            try
            {
                using var client = GetAuthenticatedClient();
                var response = await client.DeleteAsync($"{GetApiBaseUrl()}/users/{id}");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> ChangeUserStatusAsync(int id, int status)
        {
            try
            {
                using var client = GetAuthenticatedClient();
                var response = await client.PatchAsync($"{GetApiBaseUrl()}/users/{id}/status/{status}", null);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        // Enhanced User Management Methods
        public async Task<UserListResponse?> GetUsersWithPaginationAsync(UserSearchRequest request)
        {
            try
            {
                using var client = GetAuthenticatedClient();
                var queryParams = new List<string>();
                
                if (!string.IsNullOrEmpty(request.SearchTerm))
                    queryParams.Add($"searchTerm={Uri.EscapeDataString(request.SearchTerm)}");
                if (request.RoleId.HasValue)
                    queryParams.Add($"roleId={request.RoleId.Value}");
                if (request.Status.HasValue)
                    queryParams.Add($"status={request.Status.Value}");
                queryParams.Add($"page={request.Page}");
                queryParams.Add($"pageSize={request.PageSize}");
                queryParams.Add($"sortBy={request.SortBy}");
                queryParams.Add($"sortOrder={request.SortOrder}");

                var queryString = string.Join("&", queryParams);
                var response = await client.GetAsync($"{GetApiBaseUrl()}/users/search?{queryString}");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<UserListResponse>(content, new JsonSerializerOptions
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

        public async Task<UserDetailsResponse?> GetUserDetailsAsync(int id)
        {
            try
            {
                using var client = GetAuthenticatedClient();
                var response = await client.GetAsync($"{GetApiBaseUrl()}/users/{id}/details");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<UserDetailsResponse>(content, new JsonSerializerOptions
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

        public async Task<bool> CreateUserAsync(UserCreateRequest request)
        {
            try
            {
                using var client = GetAuthenticatedClient();
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync($"{GetApiBaseUrl()}/users", content);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> UpdateUserAsync(UserUpdateRequest request)
        {
            try
            {
                using var client = GetAuthenticatedClient();
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PutAsync($"{GetApiBaseUrl()}/users/{request.UserId}", content);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> ResetUserPasswordAsync(PasswordResetRequest request)
        {
            try
            {
                using var client = GetAuthenticatedClient();
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync($"{GetApiBaseUrl()}/users/{request.UserId}/reset-password", content);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<Role>?> GetRolesAsync()
        {
            try
            {
                using var client = GetAuthenticatedClient();
                var response = await client.GetAsync($"{GetApiBaseUrl()}/roles");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<List<Role>>(content, new JsonSerializerOptions
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
    }
}
