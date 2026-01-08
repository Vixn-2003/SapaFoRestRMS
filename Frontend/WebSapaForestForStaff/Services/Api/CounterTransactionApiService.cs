using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using WebSapaForestForStaff.DTOs.CounterStaff;
using WebSapaForestForStaff.Services.Api.Interfaces;

namespace WebSapaForestForStaff.Services.Api
{
    /// <summary>
    /// API Service implementation for Counter Transaction
    /// </summary>
    public class CounterTransactionApiService : ICounterTransactionApiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CounterTransactionApiService(
            HttpClient httpClient,
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _baseUrl = configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7096/api";
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<TransactionHistoryListDto?> GetTransactionHistoryAsync(TransactionFilterDto filter)
        {
            try
            {
                var token = GetToken();
                if (string.IsNullOrEmpty(token))
                {
                    return null;
                }

                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var jsonContent = JsonSerializer.Serialize(filter);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/counter/transactions/filter", content);
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<TransactionHistoryListDto>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return result;
            }
            catch
            {
                return null;
            }
        }

        public async Task<byte[]?> ExportTransactionsToExcelAsync(TransactionFilterDto filter)
        {
            try
            {
                var token = GetToken();
                if (string.IsNullOrEmpty(token))
                {
                    return null;
                }

                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var jsonContent = JsonSerializer.Serialize(filter);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/counter/transactions/export-excel", content);
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                return await response.Content.ReadAsByteArrayAsync();
            }
            catch
            {
                return null;
            }
        }

        private string? GetToken()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null) return null;

            var tokenFromSession = httpContext.Session.GetString("Token");
            if (!string.IsNullOrEmpty(tokenFromSession)) return tokenFromSession;

            return httpContext.User?.FindFirst("Token")?.Value;
        }
    }
}

