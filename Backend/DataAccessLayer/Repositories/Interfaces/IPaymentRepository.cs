using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DomainAccessLayer.Models;

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
    /// Lấy danh sách đơn hàng theo ngày (kèm đầy đủ navigation properties)
    /// </summary>
    Task<IEnumerable<Order>> GetOrdersByDateAsync(DateOnly date);

    /// <summary>
    /// Lấy toàn bộ đơn hàng (kèm đầy đủ navigation properties)
    /// </summary>
    Task<IEnumerable<Order>> GetAllOrdersWithDetailsAsync();

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

    /// <summary>
    /// Lấy transaction theo ID
    /// </summary>
    Task<Transaction?> GetTransactionByIdAsync(int transactionId);

    /// <summary>
    /// Lấy danh sách transactions theo OrderId
    /// </summary>
    Task<IEnumerable<Transaction>> GetTransactionsByOrderIdAsync(int orderId);

    /// <summary>
    /// Cập nhật transaction
    /// </summary>
    Task UpdateTransactionAsync(Transaction transaction);

    /// <summary>
    /// Lấy transaction theo TransactionCode
    /// </summary>
    Task<Transaction?> GetTransactionByCodeAsync(string transactionCode);
}

