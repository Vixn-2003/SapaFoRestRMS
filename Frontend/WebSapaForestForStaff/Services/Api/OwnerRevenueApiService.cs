using System.Text;
using System.Text.Json;
using WebSapaForestForStaff.DTOs.Owner;
using WebSapaForestForStaff.Services.Api.Interfaces;

namespace WebSapaForestForStaff.Services.Api
{
    /// <summary>
    /// API Service implementation cho Owner Revenue
    /// </summary>
    public class OwnerRevenueApiService : BaseApiService, IOwnerRevenueApiService
    {
        public OwnerRevenueApiService(
            HttpClient httpClient,
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor)
            : base(httpClient, configuration, httpContextAccessor)
        {
        }

        public async Task<(bool success, RevenueResponseDto? data, string? message)> GetRevenueDataAsync(
            RevenueFilterRequestDto request,
            CancellationToken ct = default)
        {
            try
            {
                var client = GetAuthenticatedClient();
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync($"{GetApiBaseUrl()}/owner/revenue/filter", content, ct);

                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync(ct);
                    var data = JsonSerializer.Deserialize<RevenueResponseDto>(responseJson, new JsonSerializerOptions
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

        public async Task<(bool success, RevenueResponseDto? data, string? message)> GetRevenueSummaryAsync(CancellationToken ct = default)
        {
            try
            {
                var client = GetAuthenticatedClient();
                var response = await client.GetAsync($"{GetApiBaseUrl()}/owner/revenue/summary", ct);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync(ct);
                    var data = JsonSerializer.Deserialize<RevenueResponseDto>(json, new JsonSerializerOptions
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

