using System.Net.Http;
using System.Text;
using System.Text.Json;
using WebSapaForestForStaff.DTOs;
using WebSapaForestForStaff.Services.Api.Interfaces;

namespace WebSapaForestForStaff.Services.Api
{
    /// <summary>
    /// Service for user profile API operations
    /// </summary>
    public class ProfileApiService : BaseApiService, IProfileApiService
    {
        public ProfileApiService(HttpClient httpClient, IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
            : base(httpClient, configuration, httpContextAccessor)
        {
        }

        /// <summary>
        /// Gets the current user's profile
        /// </summary>
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

        /// <summary>
        /// Updates the current user's profile
        /// </summary>
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
    }
}

