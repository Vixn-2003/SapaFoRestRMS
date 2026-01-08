using DataAccessLayer.Dbcontext;
using DataAccessLayer.Repositories.Interfaces;
using DomainAccessLayer.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories;

/// <summary>
/// Repository cho SalaryChangeRequest operations
/// </summary>
public class SalaryChangeRequestRepository : ISalaryChangeRequestRepository
{
    private readonly SapaFoRestRmsContext _context;

    public SalaryChangeRequestRepository(SapaFoRestRmsContext context)
    {
        _context = context;
    }

    public async Task<SalaryChangeRequest?> GetByIdAsync(int id)
    {
        return await _context.Set<SalaryChangeRequest>()
            .Include(s => s.Position)
            .Include(s => s.RequestedByUser)
            .Include(s => s.ApprovedByUser)
            .FirstOrDefaultAsync(s => s.RequestId == id);
    }

    public async Task<IEnumerable<SalaryChangeRequest>> GetAllAsync()
    {
        return await _context.Set<SalaryChangeRequest>()
            .Include(s => s.Position)
            .Include(s => s.RequestedByUser)
            .Include(s => s.ApprovedByUser)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    public async Task AddAsync(SalaryChangeRequest entity)
    {
        await _context.Set<SalaryChangeRequest>().AddAsync(entity);
    }

    public async Task UpdateAsync(SalaryChangeRequest entity)
    {
        _context.Set<SalaryChangeRequest>().Update(entity);
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(int id)
    {
        var request = await GetByIdAsync(id);
        if (request != null)
        {
            _context.Set<SalaryChangeRequest>().Remove(request);
        }
        await Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<SalaryChangeRequest>> GetByStatusAsync(string status)
    {
        return await _context.Set<SalaryChangeRequest>()
            .Include(s => s.Position)
            .Include(s => s.RequestedByUser)
            .Include(s => s.ApprovedByUser)
            .Where(s => s.Status == status)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<SalaryChangeRequest>> GetPendingRequestsAsync()
    {
        return await GetByStatusAsync("Pending");
    }

    public async Task<SalaryChangeRequest?> GetByIdWithDetailsAsync(int requestId)
    {
        return await _context.Set<SalaryChangeRequest>()
            .Include(s => s.Position)
            .Include(s => s.RequestedByUser)
            .Include(s => s.ApprovedByUser)
            .FirstOrDefaultAsync(s => s.RequestId == requestId);
    }

    public async Task<IEnumerable<SalaryChangeRequest>> GetByPositionIdAsync(int positionId)
    {
        return await _context.Set<SalaryChangeRequest>()
            .Include(s => s.Position)
            .Include(s => s.RequestedByUser)
            .Include(s => s.ApprovedByUser)
            .Where(s => s.PositionId == positionId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<SalaryChangeRequest>> GetByRequestedByAsync(int userId)
    {
        return await _context.Set<SalaryChangeRequest>()
            .Include(s => s.Position)
            .Include(s => s.RequestedByUser)
            .Include(s => s.ApprovedByUser)
            .Where(s => s.RequestedBy == userId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }
}

