using DataAccessLayer.Dbcontext;
using DataAccessLayer.Repositories.Interfaces;
using DomainAccessLayer.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories
{
    public class ShiftRepository : IShiftRepository
    {
        private readonly SapaFoRestRmsContext _context;

        public ShiftRepository(SapaFoRestRmsContext context)
        {
            _context = context;
        }

    public async Task<Shift?> GetByIdAsync(int id)
    {
        return await _context.Shifts
            .Include(s => s.Staff)
                .ThenInclude(st => st.User)
            .Include(s => s.HandoverToStaff)
                .ThenInclude(st => st.User)
            .FirstOrDefaultAsync(s => s.ShiftId == id);
    }

        public async Task<IEnumerable<Shift>> GetAllAsync()
        {
            return await _context.Shifts
                .Include(x => x.Template)
                .Include(x => x.Department)
                .ToListAsync();
        }

        public async Task<Shift?> GetByIdAsync(int id)
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
                .Include(x => x.Template)
                .Include(x => x.Department)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task AddAsync(Shift shift)
        {
            await _context.Shifts.AddAsync(shift);
        }

        public void Update(Shift shift)
        {
            _context.Shifts.Update(shift);
        }

        public void Delete(Shift shift)
        {
            _context.Shifts.Remove(shift);
        }

        public async Task<bool> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> IsConflictAsync(int departmentId, DateTime date, TimeSpan start, TimeSpan end, int? excludeId = null)
        {
            return await _context.Shifts.AnyAsync(s =>
                s.DepartmentId == departmentId &&
                s.Date.Date == date.Date &&
                (excludeId == null || s.Id != excludeId) &&
                start < s.EndTime &&
                end > s.StartTime
            );
        }

    public async Task<decimal> GetShiftRevenueAsync(int shiftId, CancellationToken ct = default)
    {
        var shift = await _context.Shifts.FindAsync(new object[] { shiftId }, ct);
        if (shift == null) return 0;

        // Tính tổng doanh thu từ các đơn hàng trong ca
        var dayStart = shift.StartTime ?? DateTime.MinValue;
        var dayEnd = shift.EndTime ?? DateTime.MaxValue;

        var revenue = await _context.Transactions
            .Where(t => t.CreatedAt >= dayStart && t.CreatedAt <= dayEnd &&
                       (t.Status == "Paid" || t.Status == "Success"))
            .SumAsync(t => t.Amount, ct);

        return revenue;
    }

    public async Task<int> GetShiftOrderCountAsync(int shiftId, CancellationToken ct = default)
    {
        var shift = await _context.Shifts.FindAsync(new object[] { shiftId }, ct);
        if (shift == null) return 0;

        var dayStart = shift.StartTime ?? DateTime.MinValue;
        var dayEnd = shift.EndTime ?? DateTime.MaxValue;

        var count = await _context.Orders
            .Where(o => o.CreatedAt >= dayStart && o.CreatedAt <= dayEnd)
            .CountAsync(ct);

        return count;
    }
}

