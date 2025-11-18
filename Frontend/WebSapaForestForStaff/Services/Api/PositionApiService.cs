using System.Text;
using System.Text.Json;
using WebSapaForestForStaff.DTOs;
using WebSapaForestForStaff.DTOs.Positions;
using WebSapaForestForStaff.Services.Api.Interfaces;

namespace WebSapaForestForStaff.Services.Api
{
    /// <summary>
    /// Service for position management API operations
    /// </summary>
    public class PositionApiService : BaseApiService, IPositionApiService
    {
        public PositionApiService(HttpClient httpClient, IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
            : base(httpClient, configuration, httpContextAccessor)
        {
        }

        /// <summary>
        /// Gets all positions
        /// </summary>
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

        /// <summary>
        /// Searches positions with filters and pagination
        /// </summary>
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

        /// <summary>
        /// Gets a position by ID
        /// </summary>
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

        /// <summary>
        /// Creates a new position
        /// </summary>
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

        /// <summary>
        /// Updates an existing position
        /// </summary>
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

        /// <summary>
        /// Deletes a position by ID
        /// </summary>
        public async Task<bool> DeletePositionAsync(int id)
        {
            try
            {
                var response = await SendWithAutoRefreshAsync(c => c.DeleteAsync($"{GetApiBaseUrl()}/positions/{id}"));
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        /// <summary>
        /// Changes position status (active/inactive)
        /// </summary>
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

