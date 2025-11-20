using DomainAccessLayer.Models;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories.Interfaces
{
    public interface IOrderDetailRepository : IRepository<OrderDetail>
    {
        Task<OrderDetail?> GetByIdWithMenuItemAsync(int orderDetailId);
        Task<List<OrderDetail>> GetByOrderIdAsync(int orderId);
        Task<List<OrderDetail>> GetByOrderIdsAsync(List<int> orderIds);
    }
}

