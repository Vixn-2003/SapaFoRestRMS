using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataAccessLayer.Dbcontext;
using DataAccessLayer.Repositories.Interfaces;
using DomainAccessLayer.Models;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Repositories;

/// <summary>
/// Repository cho Payment operations
/// </summary>
public class PaymentRepository : IPaymentRepository
{
    private readonly SapaFoRestRmsContext _context;

    public PaymentRepository(SapaFoRestRmsContext context)
    {
        _context = context;
    }

    public async Task<Order?> GetByIdAsync(int id)
    {
        return await _context.Orders
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.MenuItem)
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Combo)
            .Include(o => o.Customer)
                .ThenInclude(c => c!.User)
            .Include(o => o.Reservation)
                .ThenInclude(r => r.ReservationTables)
                    .ThenInclude(rt => rt.Table)
            .Include(o => o.Reservation)
                .ThenInclude(r => r.Staff)
            .Include(o => o.Transactions)
            .FirstOrDefaultAsync(o => o.OrderId == id);
    }

    public async Task<IEnumerable<Order>> GetAllAsync()
    {
        return await _context.Orders
            .Include(o => o.OrderDetails)
            .Include(o => o.Customer)
            .ToListAsync();
    }

    public async Task AddAsync(Order entity)
    {
        await _context.Orders.AddAsync(entity);
    }

    public async Task UpdateAsync(Order entity)
    {
        _context.Orders.Update(entity);
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(int id)
    {
        var order = await GetByIdAsync(id);
        if (order != null)
        {
            _context.Orders.Remove(order);
        }
        await Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    public async Task<Order?> GetOrderWithItemsAsync(int orderId)
    {
        return await _context.Orders
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.MenuItem)
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Combo)
            .Include(o => o.Customer)
                .ThenInclude(c => c!.User)
            .Include(o => o.Reservation)
                .ThenInclude(r => r.ReservationTables)
                    .ThenInclude(rt => rt.Table)
            .Include(o => o.Reservation)
                .ThenInclude(r => r.Staff)
            //  FIX: Include Reservation.Customer.User để có thể fallback khi Order.Customer.User null
            .Include(o => o.Reservation)
                .ThenInclude(r => r.Customer)
                    .ThenInclude(c => c!.User)
            .Include(o => o.Payments)
            .Include(o => o.Transactions)
                .ThenInclude(t => t.ConfirmedByUser)
            .Include(o => o.ConfirmedByStaff)
                .ThenInclude(s => s.User)
            .FirstOrDefaultAsync(o => o.OrderId == orderId);
    }

    public async Task<IEnumerable<Order>> GetOrdersByReservationIdAsync(int reservationId)
    {
        return await BuildOrderQuery()
            .Where(o => o.ReservationId == reservationId)
            .OrderBy(o => o.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Order>> GetOrdersByDateAsync(DateOnly date)
    {
        var dayStart = date.ToDateTime(TimeOnly.MinValue);
        var dayEnd = date.ToDateTime(TimeOnly.MaxValue);

        return await BuildOrderQuery()
            .Where(o => o.CreatedAt.HasValue &&
                        o.CreatedAt.Value >= dayStart &&
                        o.CreatedAt.Value <= dayEnd)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Order>> GetAllOrdersWithDetailsAsync()
    {
        return await BuildOrderQuery()
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }

    public async Task<Order?> GetOrderByCodeOrTableAsync(string? orderCode, string? tableNumber)
    {
        var query = BuildOrderQuery();

        if (!string.IsNullOrWhiteSpace(orderCode))
        {
            query = query.Where(o => o.OrderId.ToString().Contains(orderCode));
        }

        if (!string.IsNullOrWhiteSpace(tableNumber))
        {
            // Tìm kiếm qua ReservationTables -> Table.TableNumber
            query = query.Where(o => o.Reservation != null && 
                o.Reservation.ReservationTables != null &&
                o.Reservation.ReservationTables.Any(rt => 
                    rt.Table != null && 
                    rt.Table.TableNumber != null && 
                    rt.Table.TableNumber.Contains(tableNumber)));
        }

        return await query.FirstOrDefaultAsync();
    }

    private IQueryable<Order> BuildOrderQuery()
    {
        return _context.Orders
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.MenuItem)
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Combo)
            .Include(o => o.Customer)
                .ThenInclude(c => c!.User)
            .Include(o => o.Reservation)
                .ThenInclude(r => r.ReservationTables)
                    .ThenInclude(rt => rt.Table)
            .Include(o => o.Reservation)
                .ThenInclude(r => r.Staff)
            //  FIX: Include Reservation.Customer.User để có thể fallback khi Order.Customer.User null
            .Include(o => o.Reservation)
                .ThenInclude(r => r.Customer)
                    .ThenInclude(c => c!.User)
            .Include(o => o.Payments)
            .Include(o => o.Transactions)
                .ThenInclude(t => t.ConfirmedByUser);
    }

    public async Task<Transaction> SaveTransactionAsync(Transaction transaction)
    {
        await _context.Set<Transaction>().AddAsync(transaction);
        await _context.SaveChangesAsync();
        return transaction;
    }

    public async Task<Transaction?> GetTransactionBySessionIdAsync(string sessionId)
    {
        return await _context.Set<Transaction>()
            .Include(t => t.Order)
            .FirstOrDefaultAsync(t => t.SessionId == sessionId);
    }

    public async Task UpdateOrderStatusAsync(int orderId, string status)
    {
        var order = await _context.Orders.FindAsync(orderId);
        if (order != null)
        {
            order.Status = status;
            _context.Orders.Update(order);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<Transaction?> GetTransactionByIdAsync(int transactionId)
    {
        return await _context.Set<Transaction>()
            .Include(t => t.Order)
            .Include(t => t.ConfirmedByUser)
            .Include(t => t.ParentTransaction)
            .Include(t => t.ChildTransactions)
            .FirstOrDefaultAsync(t => t.TransactionId == transactionId);
    }

    public async Task<IEnumerable<Transaction>> GetTransactionsByOrderIdAsync(int orderId)
    {
        return await _context.Set<Transaction>()
            .Include(t => t.ConfirmedByUser)
            .Where(t => t.OrderId == orderId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Transaction>> GetTransactionsByReservationIdAsync(int reservationId)
    {
        return await _context.Set<Transaction>()
            .Include(t => t.ConfirmedByUser)
            .Where(t => t.ReservationId == reservationId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task UpdateTransactionAsync(Transaction transaction)
    {
        _context.Set<Transaction>().Update(transaction);
        await _context.SaveChangesAsync();
    }

    public async Task<Transaction?> GetTransactionByCodeAsync(string transactionCode)
    {
        return await _context.Set<Transaction>()
            .Include(t => t.Order)
            .Include(t => t.ConfirmedByUser)
            .FirstOrDefaultAsync(t => t.TransactionCode == transactionCode);
    }

    public async Task AddOrderHistoryAsync(OrderHistory history)
    {
        await _context.Set<OrderHistory>().AddAsync(history);
    }
    
    public async Task<OrderDetail?> GetOrderDetailByIdAsync(int orderDetailId)
    {
        return await _context.Set<OrderDetail>()
            .Include(od => od.MenuItem)
            .Include(od => od.Combo)
            .Include(od => od.Order)
            .FirstOrDefaultAsync(od => od.OrderDetailId == orderDetailId);
    }

    public async Task<IEnumerable<Transaction>> GetAllTransactionsAsync()
    {
        return await _context.Set<Transaction>()
            .Include(t => t.Order)
                .ThenInclude(o => o.Customer)
            .Include(t => t.ConfirmedByUser)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Transaction>> GetFilteredTransactionsAsync(DateTime startDate, DateTime endDate, string? paymentMethod = null, string? branchName = null)
    {
        var query = _context.Set<Transaction>()
            .Include(t => t.Order)
                .ThenInclude(o => o.Customer)
                    .ThenInclude(c => c!.User)
            .Include(t => t.ConfirmedByUser)
            .Where(t => t.Status == "Paid" && t.CompletedAt.HasValue)
            .Where(t => t.CompletedAt.Value.Date >= startDate.Date && t.CompletedAt.Value.Date <= endDate.Date);

        // Filter by payment method if specified
        if (!string.IsNullOrEmpty(paymentMethod) && paymentMethod != "ALL")
        {
            if (paymentMethod.Equals("QR", StringComparison.OrdinalIgnoreCase))
            {
                // QR in system is stored as "QRBankTransfer", "QR", or "VietQR"
                query = query.Where(t => t.PaymentMethod.Equals("QRBankTransfer", StringComparison.OrdinalIgnoreCase) ||
                                        t.PaymentMethod.Equals("QR", StringComparison.OrdinalIgnoreCase) ||
                                        t.PaymentMethod.Equals("VietQR", StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                query = query.Where(t => t.PaymentMethod.Equals(paymentMethod, StringComparison.OrdinalIgnoreCase));
            }
        }

        // TODO: Filter by branch when multi-branch is implemented
        // For now, ignore branch filter

        return await query
            .OrderByDescending(t => t.CompletedAt)
            .ToListAsync();
    }
}

