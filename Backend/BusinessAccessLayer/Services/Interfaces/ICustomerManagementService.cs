using BusinessAccessLayer.Common.Pagination;
using BusinessAccessLayer.DTOs.CustomerManagement;
using BusinessAccessLayer.DTOs.CustomerProfile;
using BusinessAccessLayer.DTOs.Customers;
using System.Threading;
using System.Threading.Tasks;

namespace BusinessAccessLayer.Services.Interfaces
{
    /// <summary>
    /// Service interface cho Customer Management Module
    /// Handles business logic for UC145, UC146, UC147
    /// </summary>
    public interface ICustomerManagementService
    {
        /// <summary>
        /// UC145 - View List Customer
        /// Get paginated list of customers with filters and search
        /// </summary>
        Task<PagedResult<CustomerListItemDto>> GetCustomersAsync(
            CustomerFilterDto filter, 
            CancellationToken ct = default);

        /// <summary>
        /// UC146 - View Customer Detail
        /// Get detailed customer information including order history, favorite dishes, etc.
        /// </summary>
        Task<CustomerDetailDto?> GetCustomerDetailAsync(
            int customerId, 
            CancellationToken ct = default);

        /// <summary>
        /// UC147 - Update VIP Status
        /// Update customer VIP status with validation and audit logging
        /// </summary>
        Task<(bool Success, string Message)> UpdateVipStatusAsync(
            CustomerVipUpdateDto dto, 
            int managerId, 
            string? ipAddress = null,
            CancellationToken ct = default);

        /// <summary>
        /// Calculate if customer meets VIP criteria
        /// VIP criteria: AverageAmountPerPerson = TotalSpending / TotalVisits / AvgPeople
        /// </summary>
        Task<(bool MeetsCriteria, decimal AverageAmountPerPerson, string Reason)> CheckVipCriteriaAsync(
            int customerId,
            CancellationToken ct = default);

        /// <summary>
        /// Get customer profile information
        /// </summary>
        Task<CustomerProfileDto?> GetCustomerProfileAsync(int customerId, CancellationToken ct = default);

        /// <summary>
        /// Update customer profile information
        /// </summary>
        Task<CustomerProfileDto?> UpdateCustomerProfileAsync(int customerId, CustomerProfileUpdateRequest request, CancellationToken ct = default);
    }
}

