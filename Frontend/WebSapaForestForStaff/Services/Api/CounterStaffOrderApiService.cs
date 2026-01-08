using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using WebSapaForestForStaff.DTOs.CounterStaff;
using WebSapaForestForStaff.Services.Api.Interfaces;

namespace WebSapaForestForStaff.Services.Api
{
    /// <summary>
    /// API Service implementation for Counter Staff Order
    /// </summary>
    public class CounterStaffOrderApiService : ICounterStaffOrderApiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CounterStaffOrderApiService(
            HttpClient httpClient,
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _baseUrl = configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7096/api";
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<List<OrderListItemDto>?> GetOrdersAsync(
            string? status = null,
            DateOnly? date = null,
            string? tableNumber = null,
            string? searchKeyword = null)
        {
            try
            {
                var token = GetToken();
                if (string.IsNullOrEmpty(token))
                {
                    return null;
                }

                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var queryParams = new List<string>();
                if (!string.IsNullOrWhiteSpace(status))
                    queryParams.Add($"status={Uri.EscapeDataString(status)}");
                if (date.HasValue)
                    queryParams.Add($"date={date.Value:yyyy-MM-dd}");
                if (!string.IsNullOrWhiteSpace(tableNumber))
                    queryParams.Add($"tableNumber={Uri.EscapeDataString(tableNumber)}");
                if (!string.IsNullOrWhiteSpace(searchKeyword))
                    queryParams.Add($"searchKeyword={Uri.EscapeDataString(searchKeyword)}");

                var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : string.Empty;
                var url = $"{_baseUrl}/counter/orders{queryString}";

                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    return new List<OrderListItemDto>();
                }

                var content = await response.Content.ReadAsStringAsync();
                var orders = JsonSerializer.Deserialize<List<OrderListItemDto>>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return orders ?? new List<OrderListItemDto>();
            }
            catch
            {
                return new List<OrderListItemDto>();
            }
        }

        public async Task<OrderListItemDto?> GetOrderSummaryAsync(int orderId)
        {
            try
            {
                var token = GetToken();
                if (string.IsNullOrEmpty(token))
                {
                    return null;
                }

                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.GetAsync($"{_baseUrl}/counter/orders/{orderId}");
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                var order = JsonSerializer.Deserialize<OrderListItemDto>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return order;
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

