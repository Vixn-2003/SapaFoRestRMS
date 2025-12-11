using System.Text;
using System.Text.Json;
using WebSapaForestForStaff.DTOs.CustomerManagement;
using WebSapaForestForStaff.Services.Api.Interfaces;

namespace WebSapaForestForStaff.Services.Api
{
    /// <summary>
    /// API Service for Customer Management Module
    /// Handles API calls for UC145, UC146, UC147
    /// </summary>
    public class CustomerManagementApiService : BaseApiService, ICustomerManagementApiService
    {
        public CustomerManagementApiService(
            HttpClient httpClient, 
            IConfiguration configuration, 
            IHttpContextAccessor httpContextAccessor)
            : base(httpClient, configuration, httpContextAccessor)
        {
        }

        /// <summary>
        /// UC145 - Get paginated list of customers with filters
        /// </summary>
        public async Task<(bool Success, CustomerListResponse? Data, string? Message)> GetCustomersAsync(CustomerFilterDto filter)
        {
            try
            {
                var client = GetAuthenticatedClient();
                
                // Build query string
                var queryParams = new List<string>();
                
                if (!string.IsNullOrWhiteSpace(filter.SearchKeyword))
                    queryParams.Add($"searchKeyword={Uri.EscapeDataString(filter.SearchKeyword)}");
                
                if (filter.IsVipOnly.HasValue)
                    queryParams.Add($"isVipOnly={filter.IsVipOnly.Value}");
                
                if (filter.MinSpending.HasValue)
                    queryParams.Add($"minSpending={filter.MinSpending.Value}");
                
                if (filter.MaxSpending.HasValue)
                    queryParams.Add($"maxSpending={filter.MaxSpending.Value}");
                
                if (filter.MinVisits.HasValue)
                    queryParams.Add($"minVisits={filter.MinVisits.Value}");
                
                queryParams.Add($"sortBy={filter.SortBy}");
                queryParams.Add($"sortDirection={filter.SortDirection}");
                queryParams.Add($"page={filter.Page}");
                queryParams.Add($"pageSize={filter.PageSize}");
                
                var queryString = string.Join("&", queryParams);
                var url = $"{GetApiBaseUrl()}/api/CustomerManagement?{queryString}";
                
                var response = await client.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();
                
                if (response.IsSuccessStatusCode)
                {
                    var apiResponse = JsonSerializer.Deserialize<ApiResponseWrapper<CustomerListResponse>>(content, new JsonSerializerOptions
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
        /// UC146 - Get customer detail by ID
        /// </summary>
        public async Task<(bool Success, CustomerDetailDto? Data, string? Message)> GetCustomerDetailAsync(int customerId)
        {
            try
            {
                var client = GetAuthenticatedClient();
                var url = $"{GetApiBaseUrl()}/api/CustomerManagement/{customerId}";
                
                var response = await client.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();
                
                if (response.IsSuccessStatusCode)
                {
                    var apiResponse = JsonSerializer.Deserialize<ApiResponseWrapper<CustomerDetailDto>>(content, new JsonSerializerOptions
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
        /// UC147 - Update VIP status
        /// </summary>
        public async Task<(bool Success, string? Message)> UpdateVipStatusAsync(CustomerVipUpdateDto dto)
        {
            try
            {
                var client = GetAuthenticatedClient();
                var url = $"{GetApiBaseUrl()}/api/CustomerManagement/{dto.CustomerId}/vip";
                
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
                    
                    return (true, apiResponse?.Message ?? "VIP status updated successfully.");
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
        /// Check if customer meets VIP criteria
        /// </summary>
        public async Task<(bool Success, VipCriteriaResponse? Data, string? Message)> CheckVipCriteriaAsync(int customerId)
        {
            try
            {
                var client = GetAuthenticatedClient();
                var url = $"{GetApiBaseUrl()}/api/CustomerManagement/{customerId}/vip-criteria";
                
                var response = await client.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();
                
                if (response.IsSuccessStatusCode)
                {
                    var apiResponse = JsonSerializer.Deserialize<ApiResponseWrapper<VipCriteriaResponse>>(content, new JsonSerializerOptions
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
    }
}

