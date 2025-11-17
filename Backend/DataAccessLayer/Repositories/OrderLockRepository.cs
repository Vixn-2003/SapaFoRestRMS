using DataAccessLayer.Dbcontext;
using DataAccessLayer.Repositories.Interfaces;
using DomainAccessLayer.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories;

/// <summary>
/// Repository cho OrderLock operations
/// </summary>
public class OrderLockRepository : IOrderLockRepository
{
    private readonly SapaFoRestRmsContext _context;

    public OrderLockRepository(SapaFoRestRmsContext context)
    {
        _context = context;
    }

    public async Task<OrderLock?> GetByIdAsync(int id)
    {
        return await _context.OrderLocks
            .Include(l => l.Order)
            .Include(l => l.LockedByUser)
            .FirstOrDefaultAsync(l => l.OrderLockId == id);
    }

    public async Task<IEnumerable<OrderLock>> GetAllAsync()
    {
        return await _context.OrderLocks
            .Include(l => l.Order)
            .Include(l => l.LockedByUser)
            .OrderByDescending(l => l.LockedAt)
            .ToListAsync();
    }

    public async Task AddAsync(OrderLock entity)
    {
        await _context.OrderLocks.AddAsync(entity);
    }

    public async Task UpdateAsync(OrderLock entity)
    {
        _context.OrderLocks.Update(entity);
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(int id)
    {
        var lockEntity = await GetByIdAsync(id);
        if (lockEntity != null)
        {
            _context.OrderLocks.Remove(lockEntity);
        }
        await Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    public async Task<OrderLock?> GetActiveLockAsync(int orderId)
    {
        var now = DateTime.UtcNow;
        return await _context.OrderLocks
            .Include(l => l.LockedByUser)
            .Where(l => l.OrderId == orderId && l.ExpiresAt > now)
            .OrderByDescending(l => l.LockedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> IsOrderLockedAsync(int orderId)
    {
        var activeLock = await GetActiveLockAsync(orderId);
        return activeLock != null;
    }

    public async Task RemoveExpiredLocksAsync()
    {
        var now = DateTime.UtcNow;
        var expiredLocks = await _context.OrderLocks
            .Where(l => l.ExpiresAt <= now)
            .ToListAsync();

        if (expiredLocks.Any())
        {
            _context.OrderLocks.RemoveRange(expiredLocks);
            await _context.SaveChangesAsync();
        }
    }

    public async Task RemoveLockAsync(int orderId)
    {
        var locks = await _context.OrderLocks
            .Where(l => l.OrderId == orderId)
            .ToListAsync();

        if (locks.Any())
        {
            _context.OrderLocks.RemoveRange(locks);
            await _context.SaveChangesAsync();
        }
    }
}

