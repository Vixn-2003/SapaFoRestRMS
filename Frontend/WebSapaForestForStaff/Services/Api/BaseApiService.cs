using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using WebSapaForestForStaff.DTOs.Auth;
using WebSapaForestForStaff.Services.Api.Interfaces;

namespace WebSapaForestForStaff.Services.Api
{
    /// <summary>
    /// Base class for API services providing common functionality for token management and HTTP requests
    /// </summary>
    public abstract class BaseApiService : IBaseApiService
    {
        private const string AccessTokenSessionKey = "Token";
        private const string RefreshTokenSessionKey = "RefreshToken";
        private const string RefreshTokenCookieKey = "sfr.refreshToken";

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
            var tokenFromSession = httpContext.Session.GetString(AccessTokenSessionKey);
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
            _httpContextAccessor.HttpContext?.Session.SetString(AccessTokenSessionKey, token);
        }

        /// <summary>
        /// Clears the authentication token and refresh token from session
        /// </summary>
        public void ClearToken()
        {
            var context = _httpContextAccessor.HttpContext;
            context?.Session.Remove(AccessTokenSessionKey);
            ClearRefreshTokenStorage();
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
                var refreshToken = GetRefreshTokenValue();
                if (string.IsNullOrEmpty(refreshToken)) return false;

                var payload = new { RefreshToken = refreshToken };
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                using var request = new HttpRequestMessage(HttpMethod.Post, $"{GetApiBaseUrl().TrimEnd('/')}/Auth/refresh-token")
                {
                    Content = content
                };

                var originalAuthHeader = _httpClient.DefaultRequestHeaders.Authorization;
                _httpClient.DefaultRequestHeaders.Authorization = null;

                var response = await _httpClient.SendAsync(request);

                _httpClient.DefaultRequestHeaders.Authorization = originalAuthHeader;

                if (!response.IsSuccessStatusCode) return false;

                var body = await response.Content.ReadAsStringAsync();
                var refreshed = JsonSerializer.Deserialize<LoginResponse>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (refreshed == null || string.IsNullOrEmpty(refreshed.Token)) return false;

                await SaveTokenToSessionAndClaimsAsync(refreshed.Token, refreshed.RefreshToken);
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

        protected void SetRefreshToken(string refreshToken, TimeSpan? lifetime = null)
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null) return;

            context.Session.SetString(RefreshTokenSessionKey, refreshToken);

            if (context.Response?.HasStarted == true) return;

            var options = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.Add(lifetime ?? TimeSpan.FromDays(7))
            };
            context.Response?.Cookies.Append(RefreshTokenCookieKey, refreshToken, options);
        }

        private string? GetRefreshTokenValue()
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null) return null;

            var fromSession = context.Session.GetString(RefreshTokenSessionKey);
            if (!string.IsNullOrEmpty(fromSession))
            {
                return fromSession;
            }

            if (context.Request?.Cookies.TryGetValue(RefreshTokenCookieKey, out var fromCookie) == true &&
                !string.IsNullOrWhiteSpace(fromCookie))
            {
                return fromCookie;
            }

            return null;
        }

        private void ClearRefreshTokenStorage()
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null) return;

            context.Session.Remove(RefreshTokenSessionKey);
            if (context.Response?.HasStarted == true) return;

            context.Response?.Cookies.Delete(RefreshTokenCookieKey);
        }

        /// <summary>
        /// Saves access token to session and re-issues authentication cookie with updated claims
        /// </summary>
        /// <param name="token">New access token</param>
        /// <param name="refreshToken">Optional refresh token</param>
        protected async Task SaveTokenToSessionAndClaimsAsync(string token, string? refreshToken = null)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return;
            }

            var context = _httpContextAccessor.HttpContext;
            if (context == null)
            {
                return;
            }

            context.Session.SetString(AccessTokenSessionKey, token);

            if (!string.IsNullOrEmpty(refreshToken))
            {
                SetRefreshToken(refreshToken);
            }

            if (context.User?.Identity?.IsAuthenticated == true && context.Response?.HasStarted != true)
            {
                var authenticateResult =
                    await context.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = authenticateResult?.Principal ?? context.User;

                if (principal?.Identity is not ClaimsIdentity identity)
                {
                    return;
                }

                var existingTokenClaim = identity.FindFirst("Token");
                if (existingTokenClaim != null)
                {
                    identity.RemoveClaim(existingTokenClaim);
                }
                identity.AddClaim(new Claim("Token", token));

                var properties = authenticateResult?.Properties ?? new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(1)
                };

                await context.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    principal,
                    properties);
            }
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

