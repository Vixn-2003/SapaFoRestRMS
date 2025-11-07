using DataAccessLayer.Dbcontext;
using DataAccessLayer.Repositories.Interfaces;
using DomainAccessLayer.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
            .Include(o => o.Customer)
            .Include(o => o.Reservation)
                .ThenInclude(r => r.ReservationTables)
                    .ThenInclude(rt => rt.Table)
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
            .Include(o => o.Customer)
            .Include(o => o.Reservation)
                .ThenInclude(r => r.ReservationTables)
                    .ThenInclude(rt => rt.Table)
            .Include(o => o.Payments)
            .FirstOrDefaultAsync(o => o.OrderId == orderId);
    }

    public async Task<IEnumerable<Order>> GetPendingOrdersAsync()
    {
        return await _context.Orders
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.MenuItem)
            .Include(o => o.Customer)
            .Include(o => o.Reservation)
                .ThenInclude(r => r.ReservationTables)
                    .ThenInclude(rt => rt.Table)
            .Where(o => o.Status == "Pending" || o.Status == "pending-payment")
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }

    public async Task<Order?> GetOrderByCodeOrTableAsync(string? orderCode, string? tableNumber)
    {
        var query = _context.Orders
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.MenuItem)
            .Include(o => o.Customer)
            .Include(o => o.Reservation)
                .ThenInclude(r => r.ReservationTables)
                    .ThenInclude(rt => rt.Table)
            .AsQueryable();

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
}

