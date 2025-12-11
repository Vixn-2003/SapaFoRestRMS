using System.Text;
using System.Text.Json;
using WebSapaForestForStaff.DTOs.Staff;
using WebSapaForestForStaff.Services.Api.Interfaces;

namespace WebSapaForestForStaff.Services.Api
{
    /// <summary>
    /// API Service for Staff Management Module
    /// Handles API calls using HttpClient
    /// </summary>
    public class StaffManagementApiService : BaseApiService, IStaffManagementApiService
    {
        public StaffManagementApiService(
            HttpClient httpClient,
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor)
            : base(httpClient, configuration, httpContextAccessor)
        {
        }

        /// <summary>
        /// UC55 - Get paginated list of staff with filters
        /// </summary>
        public async Task<(bool Success, StaffListResponse? Data, string? Message)> GetStaffListAsync(StaffFilterDto filter)
        {
            try
            {
                var client = GetAuthenticatedClient();

                // Build query string
                var queryParams = new List<string>();

                if (!string.IsNullOrWhiteSpace(filter.SearchKeyword))
                    queryParams.Add($"searchKeyword={Uri.EscapeDataString(filter.SearchKeyword)}");

                if (!string.IsNullOrWhiteSpace(filter.Position))
                    queryParams.Add($"position={Uri.EscapeDataString(filter.Position)}");

                if (filter.Status.HasValue)
                    queryParams.Add($"status={filter.Status.Value}");

                if (filter.DepartmentId.HasValue)
                    queryParams.Add($"departmentId={filter.DepartmentId.Value}");

                queryParams.Add($"sortBy={filter.SortBy}");
                queryParams.Add($"sortDirection={filter.SortDirection}");
                queryParams.Add($"page={filter.Page}");
                queryParams.Add($"pageSize={filter.PageSize}");

                var queryString = string.Join("&", queryParams);
                var url = $"{GetApiBaseUrl()}/api/StaffManagement?{queryString}";

                var response = await client.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var apiResponse = JsonSerializer.Deserialize<ApiResponseWrapper<StaffListResponse>>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    return (true, apiResponse?.Data, null);
                }

                return (false, null, $"API Error: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                return (false, null, $"Exception: {ex.Message}");
            }
        }

        /// <summary>
        /// Get staff detail by ID
        /// </summary>
        public async Task<(bool Success, StaffDetailDto? Data, string? Message)> GetStaffDetailAsync(int staffId)
        {
            try
            {
                var client = GetAuthenticatedClient();
                var url = $"{GetApiBaseUrl()}/api/StaffManagement/{staffId}";

                var response = await client.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var apiResponse = JsonSerializer.Deserialize<ApiResponseWrapper<StaffDetailDto>>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    return (true, apiResponse?.Data, null);
                }

                var errorResponse = JsonSerializer.Deserialize<ApiErrorResponse>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return (false, null, errorResponse?.Message ?? $"API Error: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                return (false, null, $"Exception: {ex.Message}");
            }
        }

        /// <summary>
        /// Create new staff
        /// </summary>
        public async Task<(bool Success, int? StaffId, string? Message)> CreateStaffAsync(StaffCreateDto dto)
        {
            try
            {
                var client = GetAuthenticatedClient();
                var url = $"{GetApiBaseUrl()}/api/StaffManagement";

                var json = JsonSerializer.Serialize(dto);
                var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(url, httpContent);
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var apiResponse = JsonSerializer.Deserialize<ApiCreateResponse>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    return (true, apiResponse?.Data?.StaffId, apiResponse?.Message ?? "Staff created successfully.");
                }

                var errorResponse = JsonSerializer.Deserialize<ApiErrorResponse>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return (false, null, errorResponse?.Message ?? $"API Error: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                return (false, null, $"Exception: {ex.Message}");
            }
        }

        /// <summary>
        /// UC56 - Update existing staff
        /// </summary>
        public async Task<(bool Success, string? Message)> UpdateStaffAsync(int staffId, StaffUpdateDto dto)
        {
            try
            {
                var client = GetAuthenticatedClient();
                var url = $"{GetApiBaseUrl()}/api/StaffManagement/{staffId}";

                var json = JsonSerializer.Serialize(dto);
                var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PutAsync(url, httpContent);
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var apiResponse = JsonSerializer.Deserialize<ApiSuccessResponse>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    return (true, apiResponse?.Message ?? "Staff updated successfully.");
                }

                var errorResponse = JsonSerializer.Deserialize<ApiErrorResponse>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return (false, errorResponse?.Message ?? $"API Error: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                return (false, $"Exception: {ex.Message}");
            }
        }

        /// <summary>
        /// UC57 - Deactivate staff
        /// </summary>
        public async Task<(bool Success, string? Message)> DeactivateStaffAsync(int staffId, StaffDeactivateDto dto)
        {
            try
            {
                var client = GetAuthenticatedClient();
                var url = $"{GetApiBaseUrl()}/api/StaffManagement/{staffId}/deactivate";

                var json = JsonSerializer.Serialize(dto);
                var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PutAsync(url, httpContent);
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var apiResponse = JsonSerializer.Deserialize<ApiSuccessResponse>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    return (true, apiResponse?.Message ?? "Staff deactivated successfully.");
                }

                var errorResponse = JsonSerializer.Deserialize<ApiErrorResponse>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return (false, errorResponse?.Message ?? $"API Error: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                return (false, $"Exception: {ex.Message}");
            }
        }

        /// <summary>
        /// Get active positions for dropdown
        /// </summary>
        public async Task<(bool Success, List<PositionDto>? Data, string? Message)> GetActivePositionsAsync()
        {
            try
            {
                var client = GetAuthenticatedClient();
                var url = $"{GetApiBaseUrl()}/api/StaffManagement/positions";

                var response = await client.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var apiResponse = JsonSerializer.Deserialize<ApiResponseWrapper<List<PositionDto>>>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    return (true, apiResponse?.Data, null);
                }

                return (false, null, $"API Error: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                return (false, null, $"Exception: {ex.Message}");
            }
        }

        // Helper classes for API responses
        private class ApiResponseWrapper<T>
        {
            public bool Success { get; set; }
            public T? Data { get; set; }
        }

        private class ApiSuccessResponse
        {
            public bool Success { get; set; }
            public string? Message { get; set; }
        }

        private class ApiErrorResponse
        {
            public bool Success { get; set; }
            public string? Message { get; set; }
        }

        private class ApiCreateResponse
        {
            public bool Success { get; set; }
            public string? Message { get; set; }
            public StaffIdData? Data { get; set; }
        }

        private class StaffIdData
        {
            public int StaffId { get; set; }
        }
    }
}

