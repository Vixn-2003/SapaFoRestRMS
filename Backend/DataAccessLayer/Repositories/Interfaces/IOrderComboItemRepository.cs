using DomainAccessLayer.Models;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories.Interfaces
{
    public interface IOrderComboItemRepository : IRepository<OrderComboItem>
    {
        Task<OrderComboItem?> GetByIdWithMenuItemAsync(int orderComboItemId);
        Task<List<OrderComboItem>> GetByOrderDetailIdAsync(int orderDetailId);
    }
}

