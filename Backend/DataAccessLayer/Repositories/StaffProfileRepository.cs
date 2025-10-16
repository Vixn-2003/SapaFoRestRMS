using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DataAccessLayer.Dbcontext;
using DataAccessLayer.Repositories.Interfaces;
using DomainAccessLayer.Models;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Repositories
{
    public class StaffProfileRepository : IStaffProfileRepository
    {
        private readonly SapaFoRestRmsContext _context;

        public StaffProfileRepository(SapaFoRestRmsContext context)
        {
            _context = context;
        }

        public async Task<List<User>> GetAllWithDetailsAsync(CancellationToken ct = default)
        {
            return await _context.Users
                .Include(u => u.Role)
                .Include(u => u.Staff)
                    .ThenInclude(s => s.Positions)
                .Where(u => u.IsDeleted == false)
                .ToListAsync(ct);
        }

        public async Task<User?> GetWithDetailsAsync(int userId, CancellationToken ct = default)
        {
            return await _context.Users
                .Include(u => u.Role)
                .Include(u => u.Staff)
                    .ThenInclude(s => s.Positions)
                .FirstOrDefaultAsync(u => u.UserId == userId && u.IsDeleted == false, ct);
        }

        public async Task UpdateAsync(User user, CancellationToken ct = default)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync(ct);
        }
    }
}


