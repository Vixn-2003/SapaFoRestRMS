using BusinessAccessLayer.Services;
using DataAccessLayer.Dbcontext;
using DomainAccessLayer.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Tests.Services.VinxTest;

/// <summary>
/// Unit Tests for AuditLogService
/// </summary>
public class AuditLogServiceTests : IDisposable
{
    private readonly SapaFoRestRmsContext _context;
    private readonly AuditLogService _auditLogService;

    public AuditLogServiceTests()
    {
        // Use in-memory database for testing
        var optionsBuilder = new DbContextOptionsBuilder<SapaFoRestRmsContext>();
        optionsBuilder.UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}");
        var options = optionsBuilder.Options;

        _context = new SapaFoRestRmsContext(options);
        
        // Ensure database is created
        _context.Database.EnsureCreated();
        
        _auditLogService = new AuditLogService(_context);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }

    #region Test 1: LogEventAsync_WithAllParameters_CreatesAuditLog

    [Fact]
    public async Task LogEventAsync_WithAllParameters_CreatesAuditLog()
    {
        // Arrange
        var eventType = "vip_status_update";
        var entityType = "Customer";
        var entityId = 123;
        var description = "Customer VIP status updated";
        var metadata = "{\"oldStatus\":false,\"newStatus\":true}";
        var userId = 10;
        var ipAddress = "192.168.1.1";

        // Act
        await _auditLogService.LogEventAsync(
            eventType, 
            entityType, 
            entityId, 
            description, 
            metadata, 
            userId, 
            ipAddress
        );

        // Assert
        var auditLog = await _context.AuditLogs.FirstOrDefaultAsync();
        Assert.NotNull(auditLog);
        Assert.Equal(eventType, auditLog.EventType);
        Assert.Equal(entityType, auditLog.EntityType);
        Assert.Equal(entityId, auditLog.EntityId);
        Assert.Equal(description, auditLog.Description);
        Assert.Equal(metadata, auditLog.Metadata);
        Assert.Equal(userId, auditLog.UserId);
        Assert.Equal(ipAddress, auditLog.IpAddress);
        Assert.True(auditLog.CreatedAt <= DateTime.UtcNow);
        Assert.True(auditLog.CreatedAt >= DateTime.UtcNow.AddSeconds(-1));
    }

    #endregion

    #region Test 2: LogEventAsync_WithMinimalParameters_CreatesAuditLog

    [Fact]
    public async Task LogEventAsync_WithMinimalParameters_CreatesAuditLog()
    {
        // Arrange
        var eventType = "payment_success";
        var entityType = "Order";
        var entityId = 456;

        // Act
        await _auditLogService.LogEventAsync(eventType, entityType, entityId);

        // Assert
        var auditLog = await _context.AuditLogs.FirstOrDefaultAsync();
        Assert.NotNull(auditLog);
        Assert.Equal(eventType, auditLog.EventType);
        Assert.Equal(entityType, auditLog.EntityType);
        Assert.Equal(entityId, auditLog.EntityId);
        Assert.Null(auditLog.Description);
        Assert.Null(auditLog.Metadata);
        Assert.Null(auditLog.UserId);
        Assert.Null(auditLog.IpAddress);
    }

    #endregion

    #region Test 3: LogEventAsync_WithLongDescription_TruncatesDescription

    [Fact]
    public async Task LogEventAsync_WithLongDescription_TruncatesDescription()
    {
        // Arrange
        var eventType = "test_event";
        var entityType = "Test";
        var entityId = 1;
        var longDescription = new string('A', 1500); // 1500 characters

        // Act
        await _auditLogService.LogEventAsync(eventType, entityType, entityId, longDescription);

        // Assert
        var auditLog = await _context.AuditLogs.FirstOrDefaultAsync();
        Assert.NotNull(auditLog);
        Assert.NotNull(auditLog.Description);
        Assert.Equal(1000, auditLog.Description.Length);
        Assert.EndsWith("...", auditLog.Description);
        Assert.Equal(new string('A', 997) + "...", auditLog.Description);
    }

    #endregion

    #region Test 4: LogEventAsync_WithDescriptionExactly1000Chars_DoesNotTruncate

    [Fact]
    public async Task LogEventAsync_WithDescriptionExactly1000Chars_DoesNotTruncate()
    {
        // Arrange
        var eventType = "test_event";
        var entityType = "Test";
        var entityId = 1;
        var description = new string('B', 1000); // Exactly 1000 characters

        // Act
        await _auditLogService.LogEventAsync(eventType, entityType, entityId, description);

        // Assert
        var auditLog = await _context.AuditLogs.FirstOrDefaultAsync();
        Assert.NotNull(auditLog);
        Assert.NotNull(auditLog.Description);
        Assert.Equal(1000, auditLog.Description.Length);
        Assert.Equal(description, auditLog.Description);
        Assert.DoesNotContain("...", auditLog.Description);
    }

    #endregion

    #region Test 5: LogEventAsync_WithDescription999Chars_DoesNotTruncate

    [Fact]
    public async Task LogEventAsync_WithDescription999Chars_DoesNotTruncate()
    {
        // Arrange
        var eventType = "test_event";
        var entityType = "Test";
        var entityId = 1;
        var description = new string('C', 999); // 999 characters

        // Act
        await _auditLogService.LogEventAsync(eventType, entityType, entityId, description);

        // Assert
        var auditLog = await _context.AuditLogs.FirstOrDefaultAsync();
        Assert.NotNull(auditLog);
        Assert.NotNull(auditLog.Description);
        Assert.Equal(999, auditLog.Description.Length);
        Assert.Equal(description, auditLog.Description);
    }

    #endregion

    #region Test 6: LogEventAsync_WithNullDescription_HandlesNull

    [Fact]
    public async Task LogEventAsync_WithNullDescription_HandlesNull()
    {
        // Arrange
        var eventType = "test_event";
        var entityType = "Test";
        var entityId = 1;
        string? description = null;

        // Act
        await _auditLogService.LogEventAsync(eventType, entityType, entityId, description);

        // Assert
        var auditLog = await _context.AuditLogs.FirstOrDefaultAsync();
        Assert.NotNull(auditLog);
        Assert.Null(auditLog.Description);
    }

    #endregion

    #region Test 7: LogEventAsync_SetsCreatedAtToUtcNow

    [Fact]
    public async Task LogEventAsync_SetsCreatedAtToUtcNow()
    {
        // Arrange
        var eventType = "test_event";
        var entityType = "Test";
        var entityId = 1;
        var beforeCall = DateTime.UtcNow;

        // Act
        await _auditLogService.LogEventAsync(eventType, entityType, entityId);
        var afterCall = DateTime.UtcNow;

        // Assert
        var auditLog = await _context.AuditLogs.FirstOrDefaultAsync();
        Assert.NotNull(auditLog);
        Assert.True(auditLog.CreatedAt >= beforeCall);
        Assert.True(auditLog.CreatedAt <= afterCall);
    }

    #endregion

    #region Test 8: LogEventAsync_WithCancellationToken_CompletesSuccessfully

    [Fact]
    public async Task LogEventAsync_WithCancellationToken_CompletesSuccessfully()
    {
        // Arrange
        var eventType = "test_event";
        var entityType = "Test";
        var entityId = 1;
        var cancellationToken = new CancellationToken();

        // Act
        await _auditLogService.LogEventAsync(
            eventType, 
            entityType, 
            entityId, 
            ct: cancellationToken
        );

        // Assert
        var auditLog = await _context.AuditLogs.FirstOrDefaultAsync();
        Assert.NotNull(auditLog);
        Assert.Equal(eventType, auditLog.EventType);
        Assert.Equal(entityType, auditLog.EntityType);
        Assert.Equal(entityId, auditLog.EntityId);
    }

    #endregion

    #region Test 9: LogEventAsync_MultipleLogs_CreatesMultipleEntries

    [Fact]
    public async Task LogEventAsync_MultipleLogs_CreatesMultipleEntries()
    {
        // Arrange
        var eventType1 = "event1";
        var eventType2 = "event2";
        var entityType = "Test";
        var entityId1 = 1;
        var entityId2 = 2;

        // Act
        await _auditLogService.LogEventAsync(eventType1, entityType, entityId1);
        await _auditLogService.LogEventAsync(eventType2, entityType, entityId2);

        // Assert
        var auditLogs = await _context.AuditLogs.ToListAsync();
        Assert.Equal(2, auditLogs.Count);
        Assert.Contains(auditLogs, log => log.EventType == eventType1 && log.EntityId == entityId1);
        Assert.Contains(auditLogs, log => log.EventType == eventType2 && log.EntityId == entityId2);
    }

    #endregion
}
