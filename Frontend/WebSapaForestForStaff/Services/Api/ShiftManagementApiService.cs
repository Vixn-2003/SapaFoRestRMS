using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using WebSapaForestForStaff.DTOs.ShiftManagement;

namespace WebSapaForestForStaff.Services.Api
{
    public interface IShiftManagementApiService
    {
        // Opening
        Task<ShiftOpeningResponseDto> DeclareOpeningBalanceAsync(ShiftOpeningDeclareRequestDto request);
        Task<ShiftDenominationsResponseDto> SubmitOpeningDenominationsAsync(ShiftOpeningDenominationsRequestDto request);
        Task<ShiftOpeningResponseDto> ConfirmShiftOpeningAsync(ShiftOpeningConfirmRequestDto request);
        
        // Closing
        Task<ShiftDenominationsResponseDto> CountClosingCashAsync(ShiftDenominationsRequestDto request);
        Task<ShiftDifferenceDto> CalculateDifferenceAsync(int shiftId, decimal actualClosingBalance);
        Task<bool> AddClosingNotesAsync(ShiftClosingNotesRequestDto request);
        Task<ShiftResponseDto> ConfirmClosingAsync(ShiftClosingConfirmRequestDto request);
        
        // Handover
        Task<List<ShiftStaffDto>> GetAvailableHandoverStaffAsync(int currentStaffId);
        Task<bool> SaveHandoverNotesAsync(ShiftHandoverNotesRequestDto request);
        Task<bool> VerifyHandoverPinAsync(ShiftHandoverPinRequestDto request);
        Task<ShiftHandoverResponseDto> CreateNextShiftAfterHandoverAsync(ShiftHandoverCreateNextRequestDto request);
        
        // Dashboard & History
        Task<ShiftDashboardDto> GetShiftDashboardAsync(int staffId);
        Task<ShiftHistoryListDto> GetShiftHistoryAsync(ShiftFilterDto filter);
        Task<ShiftDetailDto> GetShiftDetailsAsync(int shiftId);
        Task<byte[]> ExportShiftReportAsync(int shiftId);
        
        // Utility
        Task<ShiftDto> GetCurrentOpenShiftAsync(int staffId);
        Task<bool> HasOpenShiftAsync(int staffId);
        Task<ShiftDto> GetShiftByIdAsync(int shiftId);
    }

    public class ShiftManagementApiService : BaseApiService, IShiftManagementApiService
    {
        private readonly ILogger<ShiftManagementApiService> _logger;

        public ShiftManagementApiService(
            HttpClient httpClient,
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor,
            ILogger<ShiftManagementApiService> logger)
            : base(httpClient, configuration, httpContextAccessor)
        {
            _logger = logger;
        }

        private string BuildUrl(string path) => $"{GetApiBaseUrl()}/ShiftManagement{path}";

        // ========== OPENING SHIFT ==========

        public async Task<ShiftOpeningResponseDto> DeclareOpeningBalanceAsync(ShiftOpeningDeclareRequestDto request)
        {
            try
            {
                var response = await SendWithAutoRefreshAsync(client =>
                    client.PostAsJsonAsync(BuildUrl("/opening/declare"), request));

                if (!response.IsSuccessStatusCode)
                {
                    var errorMessage = await ReadApiMessageAsync(response) ?? "Không thể khai báo số dư đầu ca";
                    return new ShiftOpeningResponseDto { Success = false, Message = errorMessage };
                }

                return await response.Content.ReadFromJsonAsync<ShiftOpeningResponseDto>()
                       ?? new ShiftOpeningResponseDto { Success = false, Message = "Lỗi đọc dữ liệu" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeclareOpeningBalanceAsync");
                return new ShiftOpeningResponseDto { Success = false, Message = $"Lỗi: {ex.Message}" };
            }
        }

        public async Task<ShiftDenominationsResponseDto> SubmitOpeningDenominationsAsync(ShiftOpeningDenominationsRequestDto request)
        {
            try
            {
                var response = await SendWithAutoRefreshAsync(client =>
                    client.PostAsJsonAsync(BuildUrl("/opening/denominations"), request));

                if (!response.IsSuccessStatusCode)
                {
                    var errorMessage = await ReadApiMessageAsync(response) ?? "Không thể lưu mệnh giá";
                    return new ShiftDenominationsResponseDto { Success = false, Message = errorMessage };
                }

                return await response.Content.ReadFromJsonAsync<ShiftDenominationsResponseDto>()
                       ?? new ShiftDenominationsResponseDto { Success = false, Message = "Lỗi đọc dữ liệu" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SubmitOpeningDenominationsAsync");
                return new ShiftDenominationsResponseDto { Success = false, Message = $"Lỗi: {ex.Message}" };
            }
        }

        public async Task<ShiftOpeningResponseDto> ConfirmShiftOpeningAsync(ShiftOpeningConfirmRequestDto request)
        {
            try
            {
                var response = await SendWithAutoRefreshAsync(client =>
                    client.PostAsJsonAsync(BuildUrl("/opening/confirm"), request));

                if (!response.IsSuccessStatusCode)
                {
                    var errorMessage = await ReadApiMessageAsync(response) ?? "Không thể xác nhận mở ca";
                    return new ShiftOpeningResponseDto { Success = false, Message = errorMessage };
                }

                return await response.Content.ReadFromJsonAsync<ShiftOpeningResponseDto>()
                       ?? new ShiftOpeningResponseDto { Success = false, Message = "Lỗi đọc dữ liệu" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ConfirmShiftOpeningAsync");
                return new ShiftOpeningResponseDto { Success = false, Message = $"Lỗi: {ex.Message}" };
            }
        }

        // ========== CLOSING SHIFT ==========

        public async Task<ShiftDenominationsResponseDto> CountClosingCashAsync(ShiftDenominationsRequestDto request)
        {
            try
            {
                var response = await SendWithAutoRefreshAsync(client =>
                    client.PostAsJsonAsync(BuildUrl("/closing/denominations"), request));

                if (!response.IsSuccessStatusCode)
                {
                    var errorMessage = await ReadApiMessageAsync(response) ?? "Không thể lưu mệnh giá cuối ca";
                    return new ShiftDenominationsResponseDto { Success = false, Message = errorMessage };
                }

                return await response.Content.ReadFromJsonAsync<ShiftDenominationsResponseDto>()
                       ?? new ShiftDenominationsResponseDto { Success = false, Message = "Lỗi đọc dữ liệu" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CountClosingCashAsync");
                return new ShiftDenominationsResponseDto { Success = false, Message = $"Lỗi: {ex.Message}" };
            }
        }

        public async Task<ShiftDifferenceDto> CalculateDifferenceAsync(int shiftId, decimal actualClosingBalance)
        {
            try
            {
                var response = await SendWithAutoRefreshAsync(client =>
                    client.PostAsJsonAsync(BuildUrl("/closing/calculate"), new { ShiftId = shiftId, ActualClosingBalance = actualClosingBalance }));

                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<ShiftDifferenceDto>()
                       ?? new ShiftDifferenceDto();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CalculateDifferenceAsync");
                throw;
            }
        }

        public async Task<bool> AddClosingNotesAsync(ShiftClosingNotesRequestDto request)
        {
            try
            {
                var response = await SendWithAutoRefreshAsync(client =>
                    client.PostAsJsonAsync(BuildUrl("/closing/notes"), request));

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AddClosingNotesAsync");
                return false;
            }
        }

        public async Task<ShiftResponseDto> ConfirmClosingAsync(ShiftClosingConfirmRequestDto request)
        {
            try
            {
                var response = await SendWithAutoRefreshAsync(client =>
                    client.PostAsJsonAsync(BuildUrl("/closing/confirm"), request));

                if (!response.IsSuccessStatusCode)
                {
                    var errorMessage = await ReadApiMessageAsync(response) ?? "Không thể xác nhận kết ca";
                    return new ShiftResponseDto { Success = false, Message = errorMessage };
                }

                return await response.Content.ReadFromJsonAsync<ShiftResponseDto>()
                       ?? new ShiftResponseDto { Success = false, Message = "Lỗi đọc dữ liệu" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ConfirmClosingAsync");
                return new ShiftResponseDto { Success = false, Message = $"Lỗi: {ex.Message}" };
            }
        }

        // ========== HANDOVER ==========

        public async Task<List<ShiftStaffDto>> GetAvailableHandoverStaffAsync(int currentStaffId)
        {
            try
            {
                var response = await SendWithAutoRefreshAsync(client =>
                    client.GetAsync(BuildUrl($"/handover/staff/{currentStaffId}")));

                if (!response.IsSuccessStatusCode)
                {
                    return new List<ShiftStaffDto>();
                }

                return await response.Content.ReadFromJsonAsync<List<ShiftStaffDto>>()
                       ?? new List<ShiftStaffDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAvailableHandoverStaffAsync");
                return new List<ShiftStaffDto>();
            }
        }

        public async Task<bool> SaveHandoverNotesAsync(ShiftHandoverNotesRequestDto request)
        {
            try
            {
                var response = await SendWithAutoRefreshAsync(client =>
                    client.PostAsJsonAsync(BuildUrl("/handover/notes"), request));

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SaveHandoverNotesAsync");
                return false;
            }
        }

        public async Task<bool> VerifyHandoverPinAsync(ShiftHandoverPinRequestDto request)
        {
            try
            {
                var response = await SendWithAutoRefreshAsync(client =>
                    client.PostAsJsonAsync(BuildUrl("/handover/verify-pin"), request));

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in VerifyHandoverPinAsync");
                return false;
            }
        }

        public async Task<ShiftHandoverResponseDto> CreateNextShiftAfterHandoverAsync(ShiftHandoverCreateNextRequestDto request)
        {
            try
            {
                var response = await SendWithAutoRefreshAsync(client =>
                    client.PostAsJsonAsync(BuildUrl("/handover/create-next"), request));

                if (!response.IsSuccessStatusCode)
                {
                    var errorMessage = await ReadApiMessageAsync(response) ?? "Không thể tạo ca mới";
                    return new ShiftHandoverResponseDto { Success = false, Message = errorMessage };
                }

                return await response.Content.ReadFromJsonAsync<ShiftHandoverResponseDto>()
                       ?? new ShiftHandoverResponseDto { Success = false, Message = "Lỗi đọc dữ liệu" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CreateNextShiftAfterHandoverAsync");
                return new ShiftHandoverResponseDto { Success = false, Message = $"Lỗi: {ex.Message}" };
            }
        }

        // ========== DASHBOARD & HISTORY ==========

        public async Task<ShiftDashboardDto> GetShiftDashboardAsync(int staffId)
        {
            try
            {
                var response = await SendWithAutoRefreshAsync(client =>
                    client.GetAsync(BuildUrl($"/dashboard/{staffId}")));

                if (!response.IsSuccessStatusCode)
                {
                    return GetDemoShiftData();
                }

                return await response.Content.ReadFromJsonAsync<ShiftDashboardDto>()
                       ?? GetDemoShiftData();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetShiftDashboardAsync");
                return GetDemoShiftData();
            }
        }

        public async Task<ShiftHistoryListDto> GetShiftHistoryAsync(ShiftFilterDto filter)
        {
            try
            {
                var queryString = $"?StaffId={filter.StaffId}&FromDate={filter.FromDate:yyyy-MM-dd}&ToDate={filter.ToDate:yyyy-MM-dd}&Status={filter.Status}&PageNumber={filter.PageNumber}&PageSize={filter.PageSize}";
                var response = await SendWithAutoRefreshAsync(client =>
                    client.GetAsync(BuildUrl($"/history{queryString}")));

                if (!response.IsSuccessStatusCode)
                {
                    return new ShiftHistoryListDto();
                }

                return await response.Content.ReadFromJsonAsync<ShiftHistoryListDto>()
                       ?? new ShiftHistoryListDto();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetShiftHistoryAsync");
                return new ShiftHistoryListDto();
            }
        }

        public async Task<ShiftDetailDto> GetShiftDetailsAsync(int shiftId)
        {
            try
            {
                var response = await SendWithAutoRefreshAsync(client =>
                    client.GetAsync(BuildUrl($"/{shiftId}/details")));

                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<ShiftDetailDto>()
                       ?? throw new KeyNotFoundException($"Shift {shiftId} not found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetShiftDetailsAsync");
                throw;
            }
        }

        public async Task<byte[]> ExportShiftReportAsync(int shiftId)
        {
            try
            {
                var response = await SendWithAutoRefreshAsync(client =>
                    client.GetAsync(BuildUrl($"/{shiftId}/export")));

                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsByteArrayAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ExportShiftReportAsync");
                return Array.Empty<byte>();
            }
        }

        // ========== UTILITY ==========

        public async Task<ShiftDto> GetCurrentOpenShiftAsync(int staffId)
        {
            try
            {
                var response = await SendWithAutoRefreshAsync(client =>
                    client.GetAsync(BuildUrl($"/current/{staffId}")));

                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                return await response.Content.ReadFromJsonAsync<ShiftDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetCurrentOpenShiftAsync");
                return null;
            }
        }

        public async Task<bool> HasOpenShiftAsync(int staffId)
        {
            try
            {
                var response = await SendWithAutoRefreshAsync(client =>
                    client.GetAsync(BuildUrl($"/has-open/{staffId}")));

                if (!response.IsSuccessStatusCode)
                {
                    return false;
                }

                var result = await response.Content.ReadFromJsonAsync<HasOpenShiftResponse>();
                return result?.HasOpenShift ?? false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in HasOpenShiftAsync");
                return false;
            }
        }

        public async Task<ShiftDto> GetShiftByIdAsync(int shiftId)
        {
            try
            {
                var response = await SendWithAutoRefreshAsync(client =>
                    client.GetAsync(BuildUrl($"/{shiftId}")));

                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<ShiftDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetShiftByIdAsync");
                throw;
            }
        }

        // ========== DEMO DATA ==========

        private ShiftDashboardDto GetDemoShiftData()
        {
            return new ShiftDashboardDto
            {
                Id = "CA20241127-001",
                Cashier = "Demo User",
                StartTime = "08:00",
                CurrentTime = DateTime.Now.ToString("HH:mm"),
                StartDate = DateTime.Now.ToString("dd/MM/yyyy"),
                OpeningBalance = 500000,
                SystemCash = 1000000,
                SystemCard = 500000,
                SystemQR = 300000,
                TotalRevenue = 1800000,
                TotalOrders = 15,
                PendingOrders = 2,
                Discount = 100000,
                ServiceFee = 50000,
                Vat = 180000,
                Debt = 0,
                TotalItems = 1950000,
                Status = "open"
            };
        }

        private record HasOpenShiftResponse(bool HasOpenShift);
    }
}
