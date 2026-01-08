using DataAccessLayer.Dbcontext;
using DataAccessLayer.Repositories.Interfaces;
using DomainAccessLayer.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories;

/// <summary>
/// Repository cho AuditLog operations
/// </summary>
public class AuditLogRepository : IAuditLogRepository
{
    private readonly SapaFoRestRmsContext _context;

    public AuditLogRepository(SapaFoRestRmsContext context)
    {
        _context = context;
    }

    public async Task<AuditLog?> GetByIdAsync(int id)
    {
        return await _context.AuditLogs
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.AuditLogId == id);
    }

    public async Task<IEnumerable<AuditLog>> GetAllAsync()
    {
        return await _context.AuditLogs
            .Include(a => a.User)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task AddAsync(AuditLog entity)
    {
        await _context.AuditLogs.AddAsync(entity);
    }

    public async Task UpdateAsync(AuditLog entity)
    {
        _context.AuditLogs.Update(entity);
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(int id)
    {
        var auditLog = await GetByIdAsync(id);
        if (auditLog != null)
        {
            _context.AuditLogs.Remove(auditLog);
        }
        await Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<AuditLog>> GetByEventTypeAsync(string eventType)
    {
        return await _context.AuditLogs
            .Include(a => a.User)
            .Where(a => a.EventType == eventType)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<AuditLog>> GetByEntityAsync(string entityType, int entityId)
    {
        return await _context.AuditLogs
            .Include(a => a.User)
            .Where(a => a.EntityType == entityType && a.EntityId == entityId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<AuditLog>> GetByUserIdAsync(int userId)
    {
        return await _context.AuditLogs
            .Include(a => a.User)
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }
}

