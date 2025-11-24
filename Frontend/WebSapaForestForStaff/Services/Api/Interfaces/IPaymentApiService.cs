using WebSapaForestForStaff.DTOs.Payment;

namespace WebSapaForestForStaff.Services.Api.Interfaces
{
    public interface IPaymentApiService : IBaseApiService
    {
        Task<List<OrderDto>> GetPendingOrdersAsync();
        Task<List<OrderDto>> GetPaidOrdersAsync();
        Task<List<OrderDto>> GetOrdersByStatusAndDateAsync(string statusFilter, DateOnly date);
        Task<OrderDetailDto?> GetOrderDetailAsync(int orderId);
        Task<BaseApiService.ApiResult> ConfirmCustomerOrderAsync(ConfirmOrderRequest request);
        Task<PaymentSessionDto?> InitiatePaymentAsync(PaymentInitiateRequest request);
        Task<BaseApiService.ApiResult> ConfirmPaymentAsync(PaymentConfirmRequest request);
        Task<ReceiptFileDto?> GenerateReceiptAsync(int orderId);
    }
}

