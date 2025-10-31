using System.Text;
using System.Text.Json;
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

        // Customer Profile Methods
        public async Task<CustomerProfile?> GetCustomerProfileAsync()
        {
            try
            {
                using var client = GetAuthenticatedClient();
                var response = await client.GetAsync($"{GetApiBaseUrl()}/api/Customer/profile");

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<CustomerProfile>(responseContent, new JsonSerializerOptions
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

        public async Task<bool> UpdateCustomerProfileAsync(CustomerProfileUpdate profile)
        {
            try
            {
                using var client = GetAuthenticatedClient();
                var json = JsonSerializer.Serialize(profile);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PutAsync($"{GetApiBaseUrl()}/api/Customer/profile", content);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        // Customer Orders Methods
        public async Task<List<CustomerOrder>?> GetCustomerOrdersAsync(string? status = null)
        {
            try
            {
                using var client = GetAuthenticatedClient();
                var url = $"{GetApiBaseUrl()}/api/Customer/orders";
                if (!string.IsNullOrEmpty(status))
                {
                    url += $"?status={Uri.EscapeDataString(status)}";
                }

                var response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<List<CustomerOrder>>(responseContent, new JsonSerializerOptions
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

        // Menu and Restaurant Info Methods
        public async Task<List<MenuItemDto>?> GetMenuItemsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{GetApiBaseUrl()}/api/MenuItems");
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<List<MenuItemDto>>(responseContent, new JsonSerializerOptions
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

        public async Task<List<ComboDto>?> GetCombosAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{GetApiBaseUrl()}/api/Combos");
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<List<ComboDto>>(responseContent, new JsonSerializerOptions
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

        public async Task<List<EventDto>?> GetEventsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{GetApiBaseUrl()}/api/Events");
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<List<EventDto>>(responseContent, new JsonSerializerOptions
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
    }

    // DTOs for API responses
    public class LoginResponse
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Token { get; set; } = null!;
        public int RoleId { get; set; }
    }

    public class CustomerProfile
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public int LoyaltyPoints { get; set; }
        public string? Notes { get; set; }
    }

    public class CustomerProfileUpdate
    {
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
    }

    public class CustomerOrder
    {
        public int OrderId { get; set; }
        public DateTime OrderDate { get; set; }
        public string Status { get; set; } = null!;
        public decimal TotalAmount { get; set; }
        public List<OrderItem> Items { get; set; } = new();
    }

    public class OrderItem
    {
        public string ItemName { get; set; } = null!;
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }
}
