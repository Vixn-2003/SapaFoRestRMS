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

        public async Task<IEnumerable<Shift>> GetAllAsync()
        {
            return await _context.Shifts
                .Include(x => x.Template)
                .Include(x => x.Department)
                .ToListAsync();
        }

        public async Task<Shift?> GetByIdAsync(int id)
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
    }

}