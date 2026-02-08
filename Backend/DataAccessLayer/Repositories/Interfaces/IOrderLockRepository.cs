using DomainAccessLayer.Models;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories.Interfaces;

/// <summary>
/// Interface cho OrderLock Repository
/// </summary>
public interface IOrderLockRepository : IRepository<OrderLock>
{
    /// <summary>
    /// Lấy active lock cho order (chưa hết hạn)
    /// </summary>
    Task<OrderLock?> GetActiveLockAsync(int orderId);

    /// <summary>
    /// Kiểm tra order có đang bị lock không
    /// </summary>
    Task<bool> IsOrderLockedAsync(int orderId);

    /// <summary>
    /// Xóa tất cả locks đã hết hạn
    /// </summary>
    Task RemoveExpiredLocksAsync();

    /// <summary>
    /// Xóa lock cho order cụ thể
    /// </summary>
    Task RemoveLockAsync(int orderId);
}

