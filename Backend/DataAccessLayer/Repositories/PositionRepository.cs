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
    public class PositionRepository : IPositionRepository
    {
        private readonly SapaFoRestRmsContext _context;

        public PositionRepository(SapaFoRestRmsContext context)
        {
            _context = context;
        }

        public async Task<List<Position>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken ct = default)
        {
            return await _context.Positions
                .Where(p => ids.Contains(p.PositionId))
                .ToListAsync(ct);
        }
    }
}


