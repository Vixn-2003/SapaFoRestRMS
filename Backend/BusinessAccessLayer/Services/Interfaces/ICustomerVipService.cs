using BusinessAccessLayer.DTOs.Customers;
using DomainAccessLayer.Models;

namespace BusinessAccessLayer.Services.Interfaces
{
    public interface ICustomerVipService
    {
        Task<CustomerVipStatisticsDto> CalculateVipStatusAsync(Customer customer, CancellationToken ct = default);
        Task<CustomerVipStatisticsDto> RefreshVipStatusAsync(int customerId, bool ignoreManualOverride = false, CancellationToken ct = default);
        Task<CustomerVipStatisticsDto> UpdateVipStatusAsync(int customerId, bool isVip, CancellationToken ct = default);
        Task AutoUpdateVipWhenPaymentCompletedAsync(int orderId, CancellationToken ct = default);
        Task<CustomerVipStatisticsDto?> GetStatisticsAsync(int customerId, CancellationToken ct = default);
    }
}

