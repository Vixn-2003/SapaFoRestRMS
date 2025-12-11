using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
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
                using var content = new MultipartFormDataContent();
                content.Add(new StringContent(request.FullName), nameof(request.FullName));
                content.Add(new StringContent(request.Phone ?? string.Empty), nameof(request.Phone));

                if (request.AvatarFile != null && request.AvatarFile.Length > 0)
                {
                    var streamContent = new StreamContent(request.AvatarFile.OpenReadStream());
                    streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(request.AvatarFile.ContentType);
                    content.Add(streamContent, nameof(request.AvatarFile), request.AvatarFile.FileName);
                }
                else if (!string.IsNullOrWhiteSpace(request.AvatarUrl))
                {
                    content.Add(new StringContent(request.AvatarUrl), nameof(request.AvatarUrl));
                }

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

