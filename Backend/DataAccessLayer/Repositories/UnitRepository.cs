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
            var result = await _context.Units.ToListAsync();
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

    }
}
