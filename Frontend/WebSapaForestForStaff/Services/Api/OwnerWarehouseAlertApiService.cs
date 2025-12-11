using System.Text.Json;
using WebSapaForestForStaff.DTOs.Owner;
using WebSapaForestForStaff.Services.Api.Interfaces;

namespace WebSapaForestForStaff.Services.Api
{
    /// <summary>
    /// API Service implementation cho Owner Warehouse Alert
    /// </summary>
    public class OwnerWarehouseAlertApiService : BaseApiService, IOwnerWarehouseAlertApiService
    {
        public OwnerWarehouseAlertApiService(
            HttpClient httpClient,
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor)
            : base(httpClient, configuration, httpContextAccessor)
        {
        }

        public async Task<(bool success, WarehouseAlertResponseDto? data, string? message)> GetWarehouseAlertsAsync(CancellationToken ct = default)
        {
            try
            {
                var client = GetAuthenticatedClient();
                var response = await client.GetAsync($"{GetApiBaseUrl()}/owner/warehouse/alerts", ct);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync(ct);
                    var data = JsonSerializer.Deserialize<WarehouseAlertResponseDto>(json, new JsonSerializerOptions
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

        public async Task<(bool success, AlertSummaryCardsDto? data, string? message)> GetWarehouseAlertSummaryAsync(CancellationToken ct = default)
        {
            try
            {
                var client = GetAuthenticatedClient();
                var response = await client.GetAsync($"{GetApiBaseUrl()}/owner/warehouse/summary", ct);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync(ct);
                    var data = JsonSerializer.Deserialize<AlertSummaryCardsDto>(json, new JsonSerializerOptions
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

