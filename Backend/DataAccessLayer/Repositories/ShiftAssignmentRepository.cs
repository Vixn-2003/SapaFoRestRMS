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
    public class ShiftAssignmentRepository : IShiftAssignmentRepository
    {
        private readonly SapaFoRestRmsContext _context;

        public ShiftAssignmentRepository(SapaFoRestRmsContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ShiftAssignment>> GetAllAsync()
        {
            return await _context.ShiftAssignments
                .Include(x => x.Shift)
                .ThenInclude(s => s.Department)
                .Include(x => x.Staff)
                .ThenInclude(s => s.User)
                .ToListAsync();
        }

        public async Task<ShiftAssignment?> GetByIdAsync(int id)
        {
            return await _context.ShiftAssignments
                .Include(x => x.Shift)
                .ThenInclude(s => s.Department)
                .Include(x => x.Staff)
                .ThenInclude(s => s.User)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task AddAsync(ShiftAssignment assignment)
        {
            await _context.ShiftAssignments.AddAsync(assignment);
        }

        public void Update(ShiftAssignment assignment)
        {
            _context.ShiftAssignments.Update(assignment);
        }

        public void Delete(ShiftAssignment assignment)
        {
            _context.ShiftAssignments.Remove(assignment);
        }

        public async Task<bool> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> IsConflictAsync(int staffId, DateTime date, TimeSpan start, TimeSpan end, int? excludeId = null)
        {
            return await _context.ShiftAssignments
                .Include(sa => sa.Shift)
                .AnyAsync(sa =>
                    sa.StaffId == staffId &&
                    sa.Shift.Date.Date == date.Date &&
                    (excludeId == null || sa.Id != excludeId) &&
                    start < sa.Shift.EndTime &&
                    end > sa.Shift.StartTime
                );
        }
    }

}
