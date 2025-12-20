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
    /// Lấy tất cả đơn hàng theo ReservationId (kèm đầy đủ navigation properties)
    /// </summary>
    Task<IEnumerable<Order>> GetOrdersByReservationIdAsync(int reservationId);

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
    /// Lấy danh sách transactions theo ReservationId
    /// </summary>
    Task<IEnumerable<Transaction>> GetTransactionsByReservationIdAsync(int reservationId);

    /// <summary>
    /// Cập nhật transaction
    /// </summary>
    Task UpdateTransactionAsync(Transaction transaction);

    /// <summary>
    /// Lấy transaction theo TransactionCode
    /// </summary>
    Task<Transaction?> GetTransactionByCodeAsync(string transactionCode);

    Task AddOrderHistoryAsync(OrderHistory history);
    
    /// <summary>
    /// Lấy OrderDetail theo ID (bao gồm MenuItem và Combo)
    /// </summary>
    Task<OrderDetail?> GetOrderDetailByIdAsync(int orderDetailId);

    /// <summary>
    /// Lấy tất cả transactions (cho Owner Revenue/Dashboard)
    /// </summary>
    Task<IEnumerable<Transaction>> GetAllTransactionsAsync();

    /// <summary>
    /// Lấy transactions đã lọc theo date range và payment method (cho Revenue filtering)
    /// </summary>
    Task<IEnumerable<Transaction>> GetFilteredTransactionsAsync(DateTime startDate, DateTime endDate, string? paymentMethod = null, string? branchName = null);
}

