using System.Threading;
using System.Threading.Tasks;

namespace BusinessAccessLayer.Services.Interfaces;

/// <summary>
/// Interface cho Audit Log Service
/// </summary>
public interface IAuditLogService
{
    /// <summary>
    /// Log một event
    /// </summary>
    Task LogEventAsync(string eventType, string entityType, int entityId, 
        string? description = null, string? metadata = null, int? userId = null, 
        string? ipAddress = null, CancellationToken ct = default);
}

