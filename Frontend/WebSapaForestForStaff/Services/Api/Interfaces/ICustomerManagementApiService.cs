using WebSapaForestForStaff.DTOs.CustomerManagement;
using System.Threading.Tasks;

namespace WebSapaForestForStaff.Services.Api.Interfaces
{
    /// <summary>
    /// Interface for Customer Management API Service
    /// Handles API calls for UC145, UC146, UC147
    /// </summary>
    public interface ICustomerManagementApiService : IBaseApiService
    {
        /// <summary>
        /// UC145 - Get paginated list of customers with filters
        /// </summary>
        Task<(bool Success, CustomerListResponse? Data, string? Message)> GetCustomersAsync(CustomerFilterDto filter);

        /// <summary>
        /// UC146 - Get customer detail by ID
        /// </summary>
        Task<(bool Success, CustomerDetailDto? Data, string? Message)> GetCustomerDetailAsync(int customerId);

        /// <summary>
        /// UC147 - Update VIP status
        /// </summary>
        Task<(bool Success, string? Message)> UpdateVipStatusAsync(CustomerVipUpdateDto dto);

        /// <summary>
        /// Check if customer meets VIP criteria
        /// </summary>
        Task<(bool Success, VipCriteriaResponse? Data, string? Message)> CheckVipCriteriaAsync(int customerId);
    }

    /// <summary>
    /// Response model for customer list
    /// </summary>
    public class CustomerListResponse
    {
        public List<CustomerListItemDto> Data { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
    }

    /// <summary>
    /// Response model for VIP criteria check
    /// </summary>
    public class VipCriteriaResponse
    {
        public int CustomerId { get; set; }
        public bool MeetsCriteria { get; set; }
        public decimal AverageAmountPerPerson { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}

