using System.Security.Claims;
using System.Text;
using System.Text.Json;
using WebSapaForestForStaff.DTOs.Auth;
using WebSapaForestForStaff.Services.Api.Interfaces;

namespace WebSapaForestForStaff.Services.Api
{
    /// <summary>
    /// Base class for API services providing common functionality for token management and HTTP requests
    /// </summary>
    public abstract class BaseApiService : IBaseApiService
    {
        protected readonly HttpClient _httpClient;
        protected readonly IConfiguration _configuration;
        protected readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// API result record for operation responses
        /// </summary>
        public record ApiResult(bool Success, string? Message = null);

        protected BaseApiService(HttpClient httpClient, IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Gets the base API URL from configuration
        /// </summary>
        public string GetApiBaseUrl()
        {
            return _configuration["ApiSettings:BaseUrl"];
        }

        /// <summary>
        /// Gets the current authentication token from session or claims
        /// </summary>
        public string? GetToken()
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

        /// <summary>
        /// Sets the authentication token in session
        /// </summary>
        public void SetToken(string token)
        {
            _httpContextAccessor.HttpContext?.Session.SetString("Token", token);
        }

        /// <summary>
        /// Clears the authentication token and refresh token from session
        /// </summary>
        public void ClearToken()
        {
            _httpContextAccessor.HttpContext?.Session.Remove("Token");
            _httpContextAccessor.HttpContext?.Session.Remove("RefreshToken");
        }

        /// <summary>
        /// Gets the current logged-in user ID from claims
        /// </summary>
        /// <returns>User ID as integer, or null if not authenticated</returns>
        public int? GetCurrentUserId()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.User?.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return null;
            }

            if (int.TryParse(userIdClaim, out var userId))
            {
                return userId;
            }

            return null;
        }

        /// <summary>
        /// Gets an authenticated HttpClient with Bearer token in headers
        /// </summary>
        protected HttpClient GetAuthenticatedClient()
        {
            var token = GetToken();
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
            return _httpClient;
        }

        /// <summary>
        /// Attempts to refresh the authentication token using refresh token
        /// </summary>
        protected async Task<bool> TryRefreshTokenAsync()
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

        /// <summary>
        /// Sends HTTP request with automatic token refresh on 401 Unauthorized
        /// </summary>
        public async Task<HttpResponseMessage> SendWithAutoRefreshAsync(Func<HttpClient, Task<HttpResponseMessage>> send)
        {
            var client = GetAuthenticatedClient();
            var response = await send(client);
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                if (await TryRefreshTokenAsync())
                {
                    var client2 = GetAuthenticatedClient();
                    response = await send(client2);
                }
            }
            return response;
        }

        /// <summary>
        /// Reads API error message from response content
        /// </summary>
        protected static async Task<string?> ReadApiMessageAsync(HttpResponseMessage response)
        {
            if (response.Content == null) return null;
            try
            {
                var content = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(content)) return null;
                using var document = JsonDocument.Parse(content);
                if (document.RootElement.TryGetProperty("message", out var messageElement) &&
                    messageElement.ValueKind == JsonValueKind.String)
                {
                    return messageElement.GetString();
                }

                return content;
            }
            catch
            {
                return null;
            }
        }
    }
}

