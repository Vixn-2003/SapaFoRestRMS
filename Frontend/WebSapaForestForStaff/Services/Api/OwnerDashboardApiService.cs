using System.Text.Json;
using WebSapaForestForStaff.DTOs.Owner;
using WebSapaForestForStaff.Services.Api.Interfaces;

namespace WebSapaForestForStaff.Services.Api
{
    /// <summary>
    /// API Service implementation cho Owner Dashboard
    /// </summary>
    public class OwnerDashboardApiService : BaseApiService, IOwnerDashboardApiService
    {
        public OwnerDashboardApiService(
            HttpClient httpClient,
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor)
            : base(httpClient, configuration, httpContextAccessor)
        {
        }

        public async Task<(bool success, OwnerDashboardDto? data, string? message)> GetDashboardDataAsync(CancellationToken ct = default)
        {
            try
            {
                var client = GetAuthenticatedClient();
                var response = await client.GetAsync($"{GetApiBaseUrl()}/owner/dashboard", ct);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync(ct);
                    var data = JsonSerializer.Deserialize<OwnerDashboardDto>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    return (true, data, null);
                }

                var errorMessage = await response.Content.ReadAsStringAsync(ct);
                return (false, null, $"Lỗi: {response.StatusCode} - {errorMessage}");
            }
            catch (Exception ex)
            {
                return (false, null, $"Lỗi kết nối: {ex.Message}");
            }
        }
    }
}

