namespace WebSapaForestForStaff.Services.Api.Interfaces
{
    /// <summary>
    /// Base interface for API services providing common functionality
    /// </summary>
    public interface IBaseApiService
    {
        /// <summary>
        /// Gets the base API URL from configuration
        /// </summary>
        string GetApiBaseUrl();

        /// <summary>
        /// Sends HTTP request with automatic token refresh on 401 Unauthorized
        /// </summary>
        Task<HttpResponseMessage> SendWithAutoRefreshAsync(Func<HttpClient, Task<HttpResponseMessage>> send);

        /// <summary>
        /// Gets the current authentication token from session or claims
        /// </summary>
        string? GetToken();

        /// <summary>
        /// Sets the authentication token in session
        /// </summary>
        void SetToken(string token);

        /// <summary>
        /// Clears the authentication token and refresh token from session
        /// </summary>
        void ClearToken();

        /// <summary>
        /// Gets the current logged-in user ID from claims
        /// </summary>
        /// <returns>User ID as integer, or null if not authenticated</returns>
        int? GetCurrentUserId();
    }
}

