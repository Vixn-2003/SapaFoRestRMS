using BusinessAccessLayer.Services.Interfaces;
using DataAccessLayer.Dbcontext;
using DomainAccessLayer.Models;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace BusinessAccessLayer.Services;

/// <summary>
/// Service xử lý audit logging
/// </summary>
public class AuditLogService : IAuditLogService
{
    private readonly SapaFoRestRmsContext _context;

    public AuditLogService(SapaFoRestRmsContext context)
    {
        _context = context;
    }

    public async Task LogEventAsync(string eventType, string entityType, int entityId, 
        string? description = null, string? metadata = null, int? userId = null, 
        string? ipAddress = null, CancellationToken ct = default)
    {
        // ✅ FIX: Truncate description to max 1000 characters to prevent database truncation error
        const int maxDescriptionLength = 1000;
        var truncatedDescription = description != null && description.Length > maxDescriptionLength
            ? description.Substring(0, maxDescriptionLength - 3) + "..."
            : description;

        var auditLog = new AuditLog
        {
            EventType = eventType,
            EntityType = entityType,
            EntityId = entityId,
            Description = truncatedDescription,
            Metadata = metadata,
            UserId = userId,
            IpAddress = ipAddress,
            CreatedAt = DateTime.UtcNow
        };

        await _context.AuditLogs.AddAsync(auditLog, ct);
        await _context.SaveChangesAsync(ct);
    }
}

