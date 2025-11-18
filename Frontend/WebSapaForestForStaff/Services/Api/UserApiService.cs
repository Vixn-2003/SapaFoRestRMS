using System.Net.Http;
using System.Text;
using System.Text.Json;
using WebSapaForestForStaff.DTOs;
using WebSapaForestForStaff.DTOs.UserManagement;
using WebSapaForestForStaff.Services.Api.Interfaces;

namespace WebSapaForestForStaff.Services.Api
{
    /// <summary>
    /// Service for user management API operations
    /// </summary>
    public class UserApiService : BaseApiService, IUserApiService
    {
        public UserApiService(HttpClient httpClient, IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
            : base(httpClient, configuration, httpContextAccessor)
        {
        }

        /// <summary>
        /// Gets all users
        /// </summary>
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

        /// <summary>
        /// Gets total count of users
        /// </summary>
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

        /// <summary>
        /// Gets a user by ID
        /// </summary>
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

        /// <summary>
        /// Updates a user (legacy method using User object)
        /// </summary>
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

        /// <summary>
        /// Deletes a user by ID
        /// </summary>
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

        /// <summary>
        /// Changes user status (active/inactive)
        /// </summary>
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

        /// <summary>
        /// Gets users with pagination and search filters
        /// </summary>
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

        /// <summary>
        /// Gets detailed user information by ID
        /// </summary>
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

        /// <summary>
        /// Creates a new user
        /// </summary>
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

        /// <summary>
        /// Updates a user using UserUpdateRequest
        /// </summary>
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

        /// <summary>
        /// Resets user password by admin
        /// </summary>
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

        /// <summary>
        /// Gets all available roles
        /// </summary>
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
    }
}

