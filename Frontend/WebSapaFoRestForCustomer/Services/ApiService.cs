using System.Text;
using System.Text.Json;
using WebSapaFoRestForCustomer.DTOs;
using WebSapaFoRestForCustomer.Models;

namespace WebSapaFoRestForCustomer.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ApiService(HttpClient httpClient, IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
        }

        private string GetApiBaseUrl()
        {
            return _configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7096";
        }

        private HttpClient GetAuthenticatedClient()
        {
            var client = new HttpClient();
            client.BaseAddress = new Uri(GetApiBaseUrl());

            // Get token from claims
            var token = _httpContextAccessor.HttpContext?.User?.FindFirst("Token")?.Value;
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }

            return client;
        }

        private async Task<bool> TryRefreshTokenAsync()
        {
            try
            {
                var refreshToken = _httpContextAccessor.HttpContext?.User?.FindFirst("RefreshToken")?.Value
                    ?? _httpContextAccessor.HttpContext?.Session.GetString("RefreshToken");
                if (string.IsNullOrEmpty(refreshToken)) return false;

                var payload = new { RefreshToken = refreshToken };
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{GetApiBaseUrl()}/api/Auth/refresh-token", content);
                if (!response.IsSuccessStatusCode) return false;

                var data = await response.Content.ReadAsStringAsync();
                var refreshed = JsonSerializer.Deserialize<LoginResponse>(data, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (refreshed == null || string.IsNullOrEmpty(refreshed.Token)) return false;

                // store new tokens
                _httpContextAccessor.HttpContext?.Session.SetString("Token", refreshed.Token);
                if (!string.IsNullOrEmpty(refreshed.RefreshToken))
                {
                    _httpContextAccessor.HttpContext?.Session.SetString("RefreshToken", refreshed.RefreshToken);
                }
                return true;
            }
            catch { return false; }
        }

        private async Task<HttpResponseMessage> SendWithAutoRefreshAsync(Func<HttpClient, Task<HttpResponseMessage>> send)
        {
            using var client = GetAuthenticatedClient();
            var response = await send(client);
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                if (await TryRefreshTokenAsync())
                {
                    using var client2 = GetAuthenticatedClient();
                    response = await send(client2);
                }
            }
            return response;
        }

        // Customer Authentication Methods
        public async Task<bool> SendOtpAsync(string phone)
        {
            try
            {
                var json = JsonSerializer.Serialize(phone);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{GetApiBaseUrl()}/api/Customer/send-otp-login", content);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<LoginResponse?> VerifyOtpAsync(string phone, string code)
        {
            try
            {
                var verifyDto = new { Phone = phone, Code = code };
                var json = JsonSerializer.Serialize(verifyDto);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{GetApiBaseUrl()}/api/Customer/verify-otp-login", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<LoginResponse>(responseContent, new JsonSerializerOptions
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

        //Customer Profile Methods
        //public async Task<CustomerProfile?> GetCustomerProfileAsync()
        //{
        //    try
        //    {
        //        var response = await SendWithAutoRefreshAsync(c => c.GetAsync($"{GetApiBaseUrl()}/api/Customer/profile"));

        //        if (response.IsSuccessStatusCode)
        //        {
        //            var responseContent = await response.Content.ReadAsStringAsync();
        //            return JsonSerializer.Deserialize<CustomerProfile>(responseContent, new JsonSerializerOptions
        //            {
        //                PropertyNameCaseInsensitive = true
        //            });
        //        }
        //        return null;
        //    }
        //    catch
        //    {
        //        return null;
        //    }
        //}

        //public async Task<bool> UpdateCustomerProfileAsync(CustomerProfileUpdate profile)
        //{
        //    try
        //    {
        //        var json = JsonSerializer.Serialize(profile);
        //        var content = new StringContent(json, Encoding.UTF8, "application/json");

        //        var response = await SendWithAutoRefreshAsync(c => c.PutAsync($"{GetApiBaseUrl()}/api/Customer/profile", content));
        //        return response.IsSuccessStatusCode;
        //    }
        //    catch
        //    {
        //        return false;
        //    }
        //}

        //Customer Orders Methods
        //public async Task<List<CustomerOrder>?> GetCustomerOrdersAsync(string? status = null)
        //{
        //    try
        //    {
        //        var url = $"{GetApiBaseUrl()}/api/Customer/orders";
        //        if (!string.IsNullOrEmpty(status))
        //        {
        //            url += $"?status={Uri.EscapeDataString(status)}";
        //        }

        //        var response = await SendWithAutoRefreshAsync(c => c.GetAsync(url));

        //        if (response.IsSuccessStatusCode)
        //        {
        //            var responseContent = await response.Content.ReadAsStringAsync();
        //            return JsonSerializer.Deserialize<List<CustomerOrder>>(responseContent, new JsonSerializerOptions
        //            {
        //                PropertyNameCaseInsensitive = true
        //            });
        //        }
        //        return null;
        //    }
        //    catch
        //    {
        //        return null;
        //    }
        //}

        //Menu and Restaurant Info Methods
        //public async Task<List<MenuItemDto>?> GetMenuItemsAsync()
        //{
        //    try
        //    {
        //        var response = await _httpClient.GetAsync($"{GetApiBaseUrl()}/api/MenuItems");
        //        if (response.IsSuccessStatusCode)
        //        {
        //            var responseContent = await response.Content.ReadAsStringAsync();
        //            return JsonSerializer.Deserialize<List<MenuItemDto>>(responseContent, new JsonSerializerOptions
        //            {
        //                PropertyNameCaseInsensitive = true
        //            });
        //        }
        //        return null;
        //    }
        //    catch
        //    {
        //        return null;
        //    }
        //}

        //public async Task<List<ComboDto>?> GetCombosAsync()
        //{
        //    try
        //    {
        //        var response = await _httpClient.GetAsync($"{GetApiBaseUrl()}/api/Combos");
        //        if (response.IsSuccessStatusCode)
        //        {
        //            var responseContent = await response.Content.ReadAsStringAsync();
        //            return JsonSerializer.Deserialize<List<ComboDto>>(responseContent, new JsonSerializerOptions
        //            {
        //                PropertyNameCaseInsensitive = true
        //            });
        //        }
        //        return null;
        //    }
        //    catch
        //    {
        //        return null;
        //    }
        //}

        //public async Task<List<EventDto>?> GetEventsAsync()
        //{
        //    try
        //    {
        //        var response = await _httpClient.GetAsync($"{GetApiBaseUrl()}/api/Events");
        //        if (response.IsSuccessStatusCode)
        //        {
        //            var responseContent = await response.Content.ReadAsStringAsync();
        //            return JsonSerializer.Deserialize<List<EventDto>>(responseContent, new JsonSerializerOptions
        //            {
        //                PropertyNameCaseInsensitive = true
        //            });
        //        }
        //        return null;
        //    }
        //    catch
        //    {
        //        return null;
        //    }
        //}
    }
}
