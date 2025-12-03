using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using WebSapaForestForStaff.DTOs.ShiftManagement;

namespace WebSapaForestForStaff.Services.Api
{
    public interface IShiftManagementApiService
    {
        Task<ShiftDashboardDto> GetCurrentShiftAsync();
        Task<ShiftResponseDto> OpenShiftAsync(OpenShiftRequestDto request);
        Task<ShiftResponseDto> CloseShiftAsync(CloseShiftRequestDto request);
        Task<ShiftResponseDto> HandoverShiftAsync(HandoverShiftRequestDto request);
    }

    public class ShiftManagementApiService : IShiftManagementApiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public ShiftManagementApiService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _baseUrl = configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7000/api";
        }

        public async Task<ShiftDashboardDto> GetCurrentShiftAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/shift/current");
                
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<ShiftDashboardDto>();
                }

                // If no active shift, return empty DTO
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return new ShiftDashboardDto();
                }

                throw new HttpRequestException($"Error fetching shift data: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetCurrentShiftAsync: {ex.Message}");
                // Return demo data for development
                return GetDemoShiftData();
            }
        }

        public async Task<ShiftResponseDto> OpenShiftAsync(OpenShiftRequestDto request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/shift/open", request);
                
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<ShiftResponseDto>();
                }

                var errorMessage = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Error opening shift: {errorMessage}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in OpenShiftAsync: {ex.Message}");
                return new ShiftResponseDto
                {
                    Success = false,
                    Message = $"Lỗi khi mở ca: {ex.Message}"
                };
            }
        }

        public async Task<ShiftResponseDto> CloseShiftAsync(CloseShiftRequestDto request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/shift/close", request);
                
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<ShiftResponseDto>();
                }

                var errorMessage = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Error closing shift: {errorMessage}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in CloseShiftAsync: {ex.Message}");
                return new ShiftResponseDto
                {
                    Success = false,
                    Message = $"Lỗi khi kết ca: {ex.Message}"
                };
            }
        }

        public async Task<ShiftResponseDto> HandoverShiftAsync(HandoverShiftRequestDto request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/shift/handover", request);
                
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<ShiftResponseDto>();
                }

                var errorMessage = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Error handing over shift: {errorMessage}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in HandoverShiftAsync: {ex.Message}");
                return new ShiftResponseDto
                {
                    Success = false,
                    Message = $"Lỗi khi giao ca: {ex.Message}"
                };
            }
        }

        // Demo data for development/testing
        private ShiftDashboardDto GetDemoShiftData()
        {
            return new ShiftDashboardDto
            {
                Id = "CA20241127-001",
                Cashier = "demo",
                StartTime = "07:59",
                CurrentTime = DateTime.Now.ToString("HH:mm"),
                StartDate = DateTime.Now.ToString("dd/MM/yyyy"),
                OpeningBalance = 500000,
                SystemCash = 53723400,
                SystemCard = 3200000,
                SystemQR = 2150000,
                TotalRevenue = 53723400,
                TotalOrders = 3,
                PendingOrders = 0,
                Discount = 1338600,
                ServiceFee = 0,
                Vat = 0,
                Debt = 0,
                TotalItems = 55062000,
                Status = "open"
            };
        }
    }
}

