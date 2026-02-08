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
    public class UnitRepository : IUnitRepository
    {
        private readonly SapaFoRestRmsContext _context;

        public UnitRepository(SapaFoRestRmsContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Unit>> GetAllUnits()
        {
            var result = await _context.Units.Include(x => x.Ingredients).ToListAsync();
            return result;
        }

        public async Task<int> GetIdUnitByString(string unitName)
        {
            if (string.IsNullOrWhiteSpace(unitName))
                return 0;

            var unit = await _context.Units
                .FirstOrDefaultAsync(u => u.UnitName == unitName);

            return unit?.UnitId ?? 0;
        }

        public async Task<bool> ExistsByNameAsync(string unitName, int? excludeId = null)
        {
            return await _context.Units.AnyAsync(u =>
                u.UnitName == unitName &&
                (!excludeId.HasValue || u.UnitId != excludeId));
        }

        public async Task AddAsync(Unit unit)
        {
            _context.Units.Add(unit);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Unit unit)
        {
            _context.Units.Update(unit);
            await _context.SaveChangesAsync();
        }

        public async Task<Unit?> GetByIdAsync(int id)
        {
            return await _context.Units
                .FirstOrDefaultAsync(u => u.UnitId == id);
        }

    }
}
