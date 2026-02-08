using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using WebSapaForestForStaff.DTOs.AdminDashboard;
using WebSapaForestForStaff.Services.Api.Interfaces;

namespace WebSapaForestForStaff.Services.Api
{
    /// <summary>
    /// API Service implementation for Admin Dashboard
    /// </summary>
    public class AdminDashboardApiService : BaseApiService, IAdminDashboardApiService
    {
        public AdminDashboardApiService(
            HttpClient httpClient,
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor)
            : base(httpClient, configuration, httpContextAccessor)
        {
        }

        public async Task<AdminDashboardDto?> GetDashboardAsync()
        {
            try
            {
                var token = GetToken();
                if (string.IsNullOrEmpty(token))
                {
                    return null;
                }

                var client = GetAuthenticatedClient();
                var response = await client.GetAsync($"{GetApiBaseUrl()}/admin/dashboard");
                
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                var dashboard = JsonSerializer.Deserialize<AdminDashboardDto>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return dashboard;
            }
            catch
            {
                return null;
            }
        }

        public async Task<List<RevenuePointDto>?> GetRevenueLast7DaysAsync()
        {
            try
            {
                var token = GetToken();
                if (string.IsNullOrEmpty(token))
                {
                    return null;
                }

                var client = GetAuthenticatedClient();
                var response = await client.GetAsync($"{GetApiBaseUrl()}/admin/dashboard/revenue-7days");
                
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<List<RevenuePointDto>>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return data;
            }
            catch
            {
                return null;
            }
        }

        public async Task<List<OrderPointDto>?> GetOrdersLast7DaysAsync()
        {
            try
            {
                var token = GetToken();
                if (string.IsNullOrEmpty(token))
                {
                    return null;
                }

                var client = GetAuthenticatedClient();
                var response = await client.GetAsync($"{GetApiBaseUrl()}/admin/dashboard/orders-7days");
                
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<List<OrderPointDto>>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return data;
            }
            catch
            {
                return null;
            }
        }

        public async Task<AlertSummaryDto?> GetAlertSummaryAsync()
        {
            try
            {
                var token = GetToken();
                if (string.IsNullOrEmpty(token))
                {
                    return null;
                }

                var client = GetAuthenticatedClient();
                var response = await client.GetAsync($"{GetApiBaseUrl()}/admin/dashboard/alerts");
                
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<AlertSummaryDto>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return data;
            }
            catch
            {
                return null;
            }
        }

        public async Task<List<SystemLogDto>?> GetRecentLogsAsync()
        {
            try
            {
                var token = GetToken();
                if (string.IsNullOrEmpty(token))
                {
                    return null;
                }

                var client = GetAuthenticatedClient();
                var response = await client.GetAsync($"{GetApiBaseUrl()}/admin/dashboard/logs");
                
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<List<SystemLogDto>>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return data;
            }
            catch
            {
                return null;
            }
        }
    }
}

