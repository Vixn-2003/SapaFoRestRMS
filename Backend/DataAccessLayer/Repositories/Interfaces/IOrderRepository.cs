using DomainAccessLayer.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories.Interfaces
{
    public interface IOrderRepository : IRepository<Order>
    {
        Task<Order?> GetByIdWithDetailsAsync(int orderId);
        Task<Order?> GetByIdWithOrderDetailsAsync(int orderId);
        Task<List<Order>> GetActiveOrdersAsync();
        Task<List<Order>> GetActiveOrdersWithFullDetailsAsync();
        Task<List<Order>> GetOrdersByStatusAsync(string status);
        Task<List<Order>> GetOrdersByCustomerIdAsync(int customerId);
        Task<List<Order>> GetRecentlyFulfilledOrdersAsync(int minutesAgo);
        Task<List<Order>> GetActiveOrdersForGroupingAsync();
        Task<List<Order>> GetActiveOrdersForStationAsync();
    }
}

