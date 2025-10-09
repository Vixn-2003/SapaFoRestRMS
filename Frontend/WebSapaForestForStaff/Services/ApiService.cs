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
            return _configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7096/api";
        }

        private string? GetToken()
        {
            return _httpContextAccessor.HttpContext?.Session.GetString("Token");
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
            var client = new HttpClient();
            var token = GetToken();
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
            return client;
        }

        // Auth methods
        public async Task<LoginResponse?> LoginAsync(LoginRequest request)
        {
            try
            {
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{GetApiBaseUrl()}/auth/login", content);
                
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
                var response = await client.GetAsync($"{GetApiBaseUrl()}/users");
                
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
    }
}
