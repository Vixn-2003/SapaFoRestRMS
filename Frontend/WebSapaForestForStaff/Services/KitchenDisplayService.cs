using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using WebSapaForestForStaff.DTOs.Kitchen;

namespace WebSapaForestForStaff.Services
{
    public class KitchenDisplayService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public KitchenDisplayService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            
            var baseUrl = _configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7096/api";
            _httpClient.BaseAddress = new Uri(baseUrl);
        }

        /// <summary>
        /// Get all active orders for Sous Chef KDS screen
        /// </summary>
        public async Task<List<KitchenOrderCardDto>?> GetActiveOrdersAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/KitchenDisplay/active-orders");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<ApiResponse<List<KitchenOrderCardDto>>>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    return result?.Data;
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Get orders filtered by specific course type
        /// </summary>
        public async Task<List<KitchenOrderCardDto>?> GetOrdersByCourseTypeAsync(string courseType)
        {
            try
            {
                var encodedCourseType = Uri.EscapeDataString(courseType);
                var response = await _httpClient.GetAsync($"/KitchenDisplay/orders-by-course-type?courseType={encodedCourseType}");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<ApiResponse<List<KitchenOrderCardDto>>>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    return result?.Data;
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Update status of a single item
        /// </summary>
        public async Task<StatusUpdateResponse?> UpdateItemStatusAsync(UpdateItemStatusRequest request)
        {
            try
            {
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync("/KitchenDisplay/update-item-status", content);
                var responseContent = await response.Content.ReadAsStringAsync();
                
                if (response.IsSuccessStatusCode)
                {
                    return JsonSerializer.Deserialize<StatusUpdateResponse>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }
                else
                {
                    // Try to parse error response
                    var errorResult = JsonSerializer.Deserialize<StatusUpdateResponse>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    return errorResult;
                }
            }
            catch
            {
                return new StatusUpdateResponse { Success = false, Message = "Error calling API" };
            }
        }

        /// <summary>
        /// Mark entire order as completed
        /// </summary>
        public async Task<StatusUpdateResponse?> CompleteOrderAsync(CompleteOrderRequest request)
        {
            try
            {
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync("/KitchenDisplay/complete-order", content);
                var responseContent = await response.Content.ReadAsStringAsync();
                
                if (response.IsSuccessStatusCode)
                {
                    return JsonSerializer.Deserialize<StatusUpdateResponse>(responseContent, new JsonSerializerOptions
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
        /// Get all available course types
        /// </summary>
        public async Task<List<string>?> GetCourseTypesAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/KitchenDisplay/course-types");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<List<string>>(content, new JsonSerializerOptions
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
        /// Get grouped items by menu item
        /// </summary>
        public async Task<List<GroupedMenuItemDto>?> GetGroupedItemsByMenuItemAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/KitchenDisplay/grouped-by-item");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<ApiResponse<List<GroupedMenuItemDto>>>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    return result?.Data;
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Get station items by category name
        /// </summary>
        public async Task<StationItemsResponse?> GetStationItemsByCategoryAsync(string categoryName)
        {
            try
            {
                var encodedCategoryName = Uri.EscapeDataString(categoryName);
                var response = await _httpClient.GetAsync($"/KitchenDisplay/station-items?categoryName={encodedCategoryName}");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<ApiResponse<StationItemsResponse>>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    return result?.Data;
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Mark order detail as urgent/not urgent
        /// </summary>
        public async Task<StatusUpdateResponse?> MarkAsUrgentAsync(MarkAsUrgentRequest request)
        {
            try
            {
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync("/KitchenDisplay/mark-as-urgent", content);
                var responseContent = await response.Content.ReadAsStringAsync();
                
                if (response.IsSuccessStatusCode)
                {
                    return JsonSerializer.Deserialize<StatusUpdateResponse>(responseContent, new JsonSerializerOptions
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
        /// Get all menu categories for stations
        /// </summary>
        public async Task<List<string>?> GetStationCategoriesAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/KitchenDisplay/station-categories");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<List<string>>(content, new JsonSerializerOptions
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
        /// Get recently fulfilled orders
        /// </summary>
        public async Task<List<KitchenOrderCardDto>?> GetRecentlyFulfilledOrdersAsync(int minutesAgo = 10)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/KitchenDisplay/recently-fulfilled-orders?minutesAgo={minutesAgo}");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<ApiResponse<List<KitchenOrderCardDto>>>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    return result?.Data;
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Recall (khôi phục) an order detail
        /// </summary>
        public async Task<StatusUpdateResponse?> RecallOrderDetailAsync(RecallOrderDetailRequest request)
        {
            try
            {
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync("/KitchenDisplay/recall-order-detail", content);
                var responseContent = await response.Content.ReadAsStringAsync();
                
                if (response.IsSuccessStatusCode)
                {
                    return JsonSerializer.Deserialize<StatusUpdateResponse>(responseContent, new JsonSerializerOptions
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
        /// Helper class for API response wrapper
        /// </summary>
        private class ApiResponse<T>
        {
            public bool Success { get; set; }
            public T? Data { get; set; }
            public string? Message { get; set; }
        }
    }
}

