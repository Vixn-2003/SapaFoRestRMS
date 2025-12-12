using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DataAccessLayer.Dbcontext;
using DataAccessLayer.Repositories.Interfaces;
using DomainAccessLayer.Models;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Repositories;

/// <summary>
/// Repository cho Shift operations
/// </summary>
public class ShiftCounterRepository : IShiftCounterRepository
{
    private readonly SapaFoRestRmsContext _context;

    public ShiftCounterRepository(SapaFoRestRmsContext context)
    {
        _context = context;
    }

    public async Task<Shift?> GetByIdAsync(int id)
    {
        return await _context.Shifts
            .Include(s => s.Staff)
                .ThenInclude(st => st.User)
            .Include(s => s.Staff)
                .ThenInclude(st => st.User)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<IEnumerable<Shift>> GetAllAsync()
    {
        return await _context.Shifts
            .Include(s => s.Staff)
                .ThenInclude(st => st.User)
            .OrderByDescending(s => s.StartTime)
            .ToListAsync();
    }

    public async Task AddAsync(Shift entity)
    {
        await _context.Shifts.AddAsync(entity);
    }

    public async Task UpdateAsync(Shift entity)
    {
        _context.Shifts.Update(entity);
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(int id)
    {
        var shift = await GetByIdAsync(id);
        if (shift != null)
        {
            _context.Shifts.Remove(shift);
        }
        await Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    public async Task<Shift?> GetCurrentOpenShiftAsync(int staffId, CancellationToken ct = default)
    {
        return await _context.Shifts
            .Include(s => s.Staff)
                .ThenInclude(st => st.User)
            .Include(s => s.Staff)
                .ThenInclude(st => st.User)
            .Where(s => s.Staff.StaffId == staffId && s.Status == "Open")
            .OrderByDescending(s => s.StartTime)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IEnumerable<Shift>> GetShiftsByDateAndStaffAsync(DateOnly date, int staffId, CancellationToken ct = default)
    {
        return await _context.Shifts
            .Include(s => s.Staff)
                .ThenInclude(st => st.User)
            //.Where(s => s.Date == date && s.Staff.StaffId == staffId)
            .OrderBy(s => s.StartTime)
            .ToListAsync(ct);
    }

    public async Task<Shift?> GetShiftWithDetailsAsync(int Id, CancellationToken ct = default)
    {
        return await _context.Shifts
            .Include(s => s.Staff)
                .ThenInclude(st => st.User)
            .Include(s => s.Staff)
                .ThenInclude(st => st.User)
            .FirstOrDefaultAsync(s => s.Id == Id, ct);
    }

    public async Task<IEnumerable<Shift>> GetAllOpenShiftsAsync(CancellationToken ct = default)
    {
        return await _context.Shifts
            .Include(s => s.Staff)
                .ThenInclude(st => st.User)
            .Where(s => s.Status == "Open")
            .OrderByDescending(s => s.StartTime)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<Shift>> GetShiftHistoryAsync(int staffId, DateOnly fromDate, DateOnly toDate, CancellationToken ct = default)
    {
        return await _context.Shifts
            .Include(s => s.Staff)
                .ThenInclude(st => st.User)
            //.Where(s => s.StaffId == staffId && s.Date >= fromDate && s.Date <= toDate)
            .OrderByDescending(s => s.Date)
            .ThenByDescending(s => s.StartTime)
            .ToListAsync(ct);
    }

    public async Task<bool> HasOpenShiftAsync(int staffId, CancellationToken ct = default)
    {
        return await _context.Shifts
            .AnyAsync(s => s.Staff.StaffId == staffId && s.Status == "Open", ct);
    }

    public async Task<decimal> GetShiftRevenueAsync(int Id, CancellationToken ct = default)
    {
        var shift = await _context.Shifts.FindAsync(new object[] { Id }, ct);
        if (shift == null) return 0;

        // Tính tổng doanh thu từ các đơn hàng trong ca
        //var dayStart = shift.StartTime ?? DateTime.MinValue;
        //var dayEnd = shift.EndTime ?? DateTime.MaxValue;

        var revenue = await _context.Transactions
            //.Where(t => t.CreatedAt >= dayStart && t.CreatedAt <= dayEnd &&
            //           (t.Status == "Paid" || t.Status == "Success"))
            .SumAsync(t => t.Amount, ct);

        return revenue;
    }

    public async Task<int> GetShiftOrderCountAsync(int Id, CancellationToken ct = default)
    {
        var shift = await _context.Shifts.FindAsync(new object[] { Id }, ct);
        if (shift == null) return 0;

        //var dayStart = shift.StartTime ?? DateTime.MinValue;
        //var dayEnd = shift.EndTime ?? DateTime.MaxValue;

        var count = await _context.Orders
            //.Where(o => o.CreatedAt >= dayStart && o.CreatedAt <= dayEnd)
            .CountAsync(ct);

        return count;
    }

    /// <summary>
    /// Thêm record vào ShiftHistory
    /// </summary>
    public async Task AddHistoryAsync(ShiftHistory history, CancellationToken ct = default)
    {
        await _context.ShiftHistorys.AddAsync(history, ct);
    }

    /// <summary>
    /// Lấy danh sách history của một shift
    /// </summary>
    public async Task<List<ShiftHistory>> GetShiftHistoriesAsync(int shiftId, CancellationToken ct = default)
    {
        return await _context.ShiftHistorys
            .Where(h => h.ShiftId == shiftId)
            .OrderByDescending(h => h.ActionAt)
            .ToListAsync(ct);
    }
}