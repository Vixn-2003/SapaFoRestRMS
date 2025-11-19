using DataAccessLayer.Dbcontext;
using DataAccessLayer.Repositories.Interfaces;
using DomainAccessLayer.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories
{
    public class RoleRepository : IRoleRepository
    {
        private readonly SapaFoRestRmsContext _context;

        public RoleRepository(SapaFoRestRmsContext context)
        {
            _context = context;
        }

        public async Task<Role?> GetByIdAsync(int id)
        {
            return await _context.Roles.FindAsync(id);
        }

        public async Task<System.Collections.Generic.IEnumerable<Role>> GetAllAsync()
        {
            return await _context.Roles.ToListAsync();
        }

        public async Task AddAsync(Role entity)
        {
            await _context.Roles.AddAsync(entity);
        }

        public async Task UpdateAsync(Role entity)
        {
            _context.Roles.Update(entity);
        }

        public async Task DeleteAsync(int id)
        {
            var role = await GetByIdAsync(id);
            if (role != null)
            {
                _context.Roles.Remove(role);
            }
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task<Role?> GetByNameAsync(string roleName, CancellationToken ct = default)
        {
            return await _context.Roles
                .FirstOrDefaultAsync(r => r.RoleName == roleName, ct);
        }

        public async Task<List<Role>> GetAllOrderedAsync(CancellationToken ct = default)
        {
            return await _context.Roles
                .OrderBy(r => r.RoleId)
                .ToListAsync(ct);
        }
    }
}

