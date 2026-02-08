using DomainAccessLayer.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories.Interfaces;

/// <summary>
/// Interface cho AuditLog Repository
/// </summary>
public interface IAuditLogRepository : IRepository<AuditLog>
{
    /// <summary>
    /// Lấy danh sách audit logs theo event type
    /// </summary>
    Task<IEnumerable<AuditLog>> GetByEventTypeAsync(string eventType);

    /// <summary>
    /// Lấy danh sách audit logs theo entity
    /// </summary>
    Task<IEnumerable<AuditLog>> GetByEntityAsync(string entityType, int entityId);

    /// <summary>
    /// Lấy danh sách audit logs theo user
    /// </summary>
    Task<IEnumerable<AuditLog>> GetByUserIdAsync(int userId);
}

