using DomainAccessLayer.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories.Interfaces;

/// <summary>
/// Interface cho Payment Repository
/// </summary>
public interface IPaymentRepository : IRepository<Order>
{
    /// <summary>
    /// Lấy đơn hàng kèm danh sách món ăn
    /// </summary>
    Task<Order?> GetOrderWithItemsAsync(int orderId);

    /// <summary>
    /// Lấy danh sách đơn hàng chờ thanh toán
    /// </summary>
    Task<IEnumerable<Order>> GetPendingOrdersAsync();

    /// <summary>
    /// Lấy đơn hàng theo mã đơn hoặc số bàn
    /// </summary>
    Task<Order?> GetOrderByCodeOrTableAsync(string? orderCode, string? tableNumber);

    /// <summary>
    /// Lưu giao dịch thanh toán
    /// </summary>
    Task<Transaction> SaveTransactionAsync(Transaction transaction);

    /// <summary>
    /// Lấy giao dịch theo sessionId
    /// </summary>
    Task<Transaction?> GetTransactionBySessionIdAsync(string sessionId);

    /// <summary>
    /// Cập nhật trạng thái đơn hàng
    /// </summary>
    Task UpdateOrderStatusAsync(int orderId, string status);
}

