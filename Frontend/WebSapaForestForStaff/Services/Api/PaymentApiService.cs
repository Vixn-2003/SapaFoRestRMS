using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using WebSapaForestForStaff.DTOs.Payment;
using WebSapaForestForStaff.Services.Api.Interfaces;

namespace WebSapaForestForStaff.Services.Api
{
    public class PaymentApiService : BaseApiService, IPaymentApiService
    {
        private readonly ILogger<PaymentApiService> _logger;

        public PaymentApiService(HttpClient httpClient, IConfiguration configuration, IHttpContextAccessor accessor, ILogger<PaymentApiService> logger)
            : base(httpClient, configuration, accessor)
        {
            _logger = logger;
        }

        public async Task<List<OrderDto>> GetPendingOrdersAsync()
        {
            return await FetchOrdersByStatusAsync("pending");
        }

        public async Task<List<OrderDto>> GetPaidOrdersAsync()
        {
            return await FetchOrdersByStatusAsync("processed");
        }

        public async Task<List<OrderDto>> GetOrdersByStatusAndDateAsync(string statusFilter, DateOnly date)
        {
            var response = await SendWithAutoRefreshAsync(client =>
                client.GetAsync(BuildApiUrl($"/payment/orders?date={date:yyyy-MM-dd}&status={statusFilter}")));

            if (!response.IsSuccessStatusCode)
            {
                return new List<OrderDto>();
            }

            var data = await response.Content.ReadFromJsonAsync<OrderListResponseDto>();
            return data?.Orders ?? new List<OrderDto>();
        }

        public async Task<OrderDetailDto?> GetOrderDetailAsync(int orderId)
        {
            var response = await SendWithAutoRefreshAsync(client =>
                client.GetAsync(BuildApiUrl($"/payment/orders/{orderId}/details")));

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<OrderDetailDto>();
        }

        /// <summary>
        /// ⚠️ KHÔNG DÙNG CHO CASHIER FLOW NỮA
        /// Method này có thể dùng cho waiter flow hoặc mục đích khác
        /// Cashier KHÔNG xác nhận món, chỉ xử lý thanh toán
        /// </summary>
        public async Task<ApiResult> ConfirmCustomerOrderAsync(ConfirmOrderRequest request)
        {
            var response = await SendWithAutoRefreshAsync(client =>
                client.PutAsJsonAsync(BuildApiUrl($"/payment/orders/{request.OrderId}/confirm"), request));

            if (!response.IsSuccessStatusCode)
            {
                var message = await ReadApiMessageAsync(response) ?? "Không thể cập nhật đơn hàng";
                return new ApiResult(false, message);
            }

            return new ApiResult(true, "Xác nhận món thành công");
        }

        public async Task<PaymentSessionDto?> InitiatePaymentAsync(PaymentInitiateRequest request)
        {
            var response = await SendWithAutoRefreshAsync(client =>
                client.PostAsJsonAsync(BuildApiUrl("/payment/payments/initiate"), request));

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<PaymentSessionDto>();
        }

        public async Task<ApiResult> ConfirmPaymentAsync(PaymentConfirmRequest request)
        {
            var response = await SendWithAutoRefreshAsync(client =>
                client.PostAsJsonAsync(BuildApiUrl("/payment/payments/confirm"), request));

            if (!response.IsSuccessStatusCode)
            {
                var message = await ReadApiMessageAsync(response) ?? "Thanh toán thất bại";
                return new ApiResult(false, message);
            }

            return new ApiResult(true, "Thanh toán thành công");
        }

        public async Task<ReceiptFileDto?> GenerateReceiptAsync(int orderId)
        {
            var requestUrl = BuildApiUrl($"/payment/receipt/{orderId}");
            var result = new ReceiptFileDto
            {
                FileName = $"receipt-{orderId}.pdf"
            };

            var response = await SendWithAutoRefreshAsync(client => client.GetAsync(requestUrl));
            result.StatusCode = response.StatusCode;
            result.ContentType = response.Content.Headers.ContentType?.MediaType;

            _logger.LogInformation("Receipt download call for order {OrderId} returned {StatusCode} with content-type {ContentType}", orderId, response.StatusCode, result.ContentType ?? "unknown");

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    _logger.LogWarning("Receipt download unauthorized for order {OrderId}", orderId);
                }

                result.ErrorMessage = await ReadApiMessageAsync(response) ?? $"Không thể tải hóa đơn (HTTP {(int)response.StatusCode})";
                _logger.LogWarning("Receipt download for order {OrderId} failed: {Error}", orderId, result.ErrorMessage);
                return result;
            }

            if (!string.Equals(result.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase))
            {
                var body = await response.Content.ReadAsStringAsync();
                result.ErrorMessage = !string.IsNullOrWhiteSpace(body)
                    ? body
                    : "Máy chủ không trả về file PDF.";

                _logger.LogWarning("Receipt download for order {OrderId} returned unexpected content-type {ContentType}. Body: {Body}", orderId, result.ContentType, body);
                return result;
            }

            var fileBytes = await response.Content.ReadAsByteArrayAsync();
            var fileName = response.Content.Headers.ContentDisposition?.FileName?.Trim('"');

            result.FileBytes = fileBytes;
            result.FileName = string.IsNullOrWhiteSpace(fileName) ? result.FileName : fileName;
            result.Success = fileBytes.Length > 0;

            if (!result.Success)
            {
                result.ErrorMessage = "File hóa đơn bị trống.";
                _logger.LogWarning("Receipt download for order {OrderId} returned empty payload.", orderId);
            }
            else
            {
                _logger.LogInformation("Receipt download for order {OrderId} succeeded with {ByteCount} bytes.", orderId, fileBytes.Length);
            }

            return result;
        }

        public async Task<DiscountApplyResponse?> ApplyDiscountAsync(DiscountRequest request)
        {
            var response = await SendWithAutoRefreshAsync(client =>
                client.PostAsJsonAsync(BuildApiUrl("/payment/discounts/validate"), request));

            var result = new DiscountApplyResponse();

            if (!response.IsSuccessStatusCode)
            {
                result.Success = false;
                result.Message = await ReadApiMessageAsync(response) ?? "Không thể áp dụng ưu đãi";
                return result;
            }

            var payload = await response.Content.ReadFromJsonAsync<DiscountApplyResponse>();
            return payload;
        }

        private async Task<List<OrderDto>> FetchOrdersByStatusAsync(string statusFilter)
        {
            var response = await SendWithAutoRefreshAsync(client =>
                client.GetAsync(BuildApiUrl($"/payment/orders?status={statusFilter}")));

            if (!response.IsSuccessStatusCode)
            {
                return new List<OrderDto>();
            }

            var data = await response.Content.ReadFromJsonAsync<OrderListResponseDto>();
            return data?.Orders ?? new List<OrderDto>();
        }
        private string BuildApiUrl(string relativePath)
        {
            var baseUrl = GetApiBaseUrl().TrimEnd('/');
            var path = relativePath.StartsWith("/") ? relativePath : $"/{relativePath}";
            return $"{baseUrl}{path}";
        }
    }
}

