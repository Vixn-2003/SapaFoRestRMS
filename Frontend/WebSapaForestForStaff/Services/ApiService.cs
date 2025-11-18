using System.Text;
using System.Text.Json;
using WebSapaForestForStaff.DTOs;
using WebSapaForestForStaff.DTOs.Auth;
using WebSapaForestForStaff.DTOs.UserManagement;
using WebSapaForestForStaff.DTOs.Positions;

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
            _httpContextAccessor.HttpContext?.Session.Remove("RefreshToken");
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

        private async Task<bool> TryRefreshTokenAsync()
        {
            try
            {
                var refreshToken = _httpContextAccessor.HttpContext?.Session.GetString("RefreshToken");
                if (string.IsNullOrEmpty(refreshToken)) return false;

                var payload = new { RefreshToken = refreshToken };
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{GetApiBaseUrl()}/Auth/refresh-token", content);
                if (!response.IsSuccessStatusCode) return false;

                var body = await response.Content.ReadAsStringAsync();
                var refreshed = JsonSerializer.Deserialize<LoginResponse>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (refreshed == null || string.IsNullOrEmpty(refreshed.Token)) return false;

                SetToken(refreshed.Token);
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

        public void Logout()
        {
            ClearToken();
        }

        // User management methods
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

        public async Task<bool> CreateStaffAsync(CreateStaffRequest request)
        {
            try
            {
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await SendWithAutoRefreshAsync(c => c.PostAsync($"{GetApiBaseUrl()}/auth/manager/create-staff", content));
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
                var response = await SendWithAutoRefreshAsync(c => c.GetAsync($"{GetApiBaseUrl()}/Users"));
                
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
                var response = await SendWithAutoRefreshAsync(c => c.GetAsync($"{GetApiBaseUrl()}/users"));
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
                var response = await SendWithAutoRefreshAsync(c => c.GetAsync($"{GetApiBaseUrl()}/events"));
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
                var response = await SendWithAutoRefreshAsync(c => c.GetAsync($"{GetApiBaseUrl()}/reservationstaff/reservations/pending-confirmed?page=1&pageSize=1"));
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
                var response = await SendWithAutoRefreshAsync(c => c.GetAsync($"{GetApiBaseUrl()}/users/{id}"));
                
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
                var json = JsonSerializer.Serialize(user);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await SendWithAutoRefreshAsync(c => c.PutAsync($"{GetApiBaseUrl()}/users/{user.UserId}", content));
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
                var response = await SendWithAutoRefreshAsync(c => c.DeleteAsync($"{GetApiBaseUrl()}/users/{id}"));
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
                var response = await SendWithAutoRefreshAsync(c => c.PatchAsync($"{GetApiBaseUrl()}/users/{id}/status/{status}", null));
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
                var response = await SendWithAutoRefreshAsync(c => c.GetAsync($"{GetApiBaseUrl()}/users/search?{queryString}"));
                
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
                var response = await SendWithAutoRefreshAsync(c => c.GetAsync($"{GetApiBaseUrl()}/users/{id}/details"));
                
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
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await SendWithAutoRefreshAsync(c => c.PostAsync($"{GetApiBaseUrl()}/users", content));
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
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await SendWithAutoRefreshAsync(c => c.PutAsync($"{GetApiBaseUrl()}/users/{request.UserId}", content));
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
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await SendWithAutoRefreshAsync(c => c.PostAsync($"{GetApiBaseUrl()}/users/{request.UserId}/reset-password", content));
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
                var response = await SendWithAutoRefreshAsync(c => c.GetAsync($"{GetApiBaseUrl()}/roles"));
                
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

        // User Profile Methods
        public async Task<User?> GetUserProfileAsync()
        {
            try
            {
                var response = await SendWithAutoRefreshAsync(c => c.GetAsync($"{GetApiBaseUrl()}/users/profile"));
                
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

        public async Task<User?> UpdateUserProfileAsync(UserProfileUpdateRequest request)
        {
            try
            {
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await SendWithAutoRefreshAsync(c => c.PutAsync($"{GetApiBaseUrl()}/users/profile", content));
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<User>(responseContent, new JsonSerializerOptions
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

        public async Task<List<Position>?> GetPositionsAsync()
        {
            try
            {
                var response = await SendWithAutoRefreshAsync(c => c.GetAsync($"{GetApiBaseUrl()}/positions"));
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<List<Position>>(content, new JsonSerializerOptions
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

        // Positions Management
        public async Task<PositionListResponse?> SearchPositionsAsync(PositionSearchRequest request)
        {
            try
            {
                var query = new List<string>();
                if (!string.IsNullOrEmpty(request.SearchTerm)) query.Add($"searchTerm={Uri.EscapeDataString(request.SearchTerm)}");
                if (request.Status.HasValue) query.Add($"status={request.Status.Value}");
                query.Add($"page={request.Page}");
                query.Add($"pageSize={request.PageSize}");
                var qs = string.Join("&", query);
                var response = await SendWithAutoRefreshAsync(c => c.GetAsync($"{GetApiBaseUrl()}/positions/search?{qs}"));
                if (!response.IsSuccessStatusCode) return null;
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<PositionListResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch { return null; }
        }

        public async Task<PositionDto?> GetPositionAsync(int id)
        {
            try
            {
                var response = await SendWithAutoRefreshAsync(c => c.GetAsync($"{GetApiBaseUrl()}/positions/{id}"));
                if (!response.IsSuccessStatusCode) return null;
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<PositionDto>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch { return null; }
        }

        public async Task<bool> CreatePositionAsync(PositionCreateRequest request)
        {
            try
            {
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await SendWithAutoRefreshAsync(c => c.PostAsync($"{GetApiBaseUrl()}/positions", content));
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        public async Task<bool> UpdatePositionAsync(PositionUpdateRequest request)
        {
            try
            {
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await SendWithAutoRefreshAsync(c => c.PutAsync($"{GetApiBaseUrl()}/positions/{request.PositionId}", content));
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        public async Task<bool> DeletePositionAsync(int id)
        {
            try
            {
                var response = await SendWithAutoRefreshAsync(c => c.DeleteAsync($"{GetApiBaseUrl()}/positions/{id}"));
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        public async Task<bool> ChangePositionStatusAsync(int id, int status)
        {
            try
            {
                var response = await SendWithAutoRefreshAsync(c => c.PatchAsync($"{GetApiBaseUrl()}/positions/{id}/status/{status}", null));
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }
    }
}
