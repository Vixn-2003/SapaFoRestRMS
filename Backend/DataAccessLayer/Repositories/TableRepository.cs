using DataAccessLayer.Dbcontext;
using DataAccessLayer.Repositories.Interfaces;
using DomainAccessLayer.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories
{
    public class TableRepository : ITableRepository
    {
        private readonly SapaFoRestRmsContext _context;

        public TableRepository(SapaFoRestRmsContext context)
        {
            _context = context;
        }

       public async Task<IEnumerable<Table>> GetAllAsync(
    string? search,
    int? capacity,
    int? areaId,
    int page,
    int pageSize,
    string? status)
{
    var query = _context.Tables
        .Include(t => t.Area)
        .AsQueryable();

    if (!string.IsNullOrEmpty(search))
        query = query.Where(t => t.TableNumber.Contains(search));

    if (capacity.HasValue)
        query = query.Where(t => t.Capacity == capacity.Value);

    if (areaId.HasValue)
        query = query.Where(t => t.AreaId == areaId.Value);

    if (!string.IsNullOrEmpty(status))
        query = query.Where(t => t.Status == status);

    return await query
        .OrderBy(t => t.TableNumber)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();
}


        public async Task<int> GetCountAsync(string? search, int? capacity, int? areaId)
        {
            var query = _context.Tables.AsQueryable();

            if (!string.IsNullOrEmpty(search))
                query = query.Where(t => t.TableNumber.Contains(search));

            if (capacity.HasValue)
                query = query.Where(t => t.Capacity == capacity.Value);

            if (areaId.HasValue)
                query = query.Where(t => t.AreaId == areaId.Value);

            return await query.CountAsync();
        }

        public async Task<Table?> GetByIdAsync(int id)
        {
            return await _context.Tables
                .Include(t => t.Area)
                .FirstOrDefaultAsync(t => t.TableId == id);
        }

        public async Task AddAsync(Table table)
        {
            await _context.Tables.AddAsync(table);
        }

        public async Task UpdateAsync(Table table)
        {
            _context.Tables.Update(table);
        }

        public async Task DeleteAsync(int id)
        {
            var table = await _context.Tables.FindAsync(id);
            if (table != null)
                _context.Tables.Remove(table);
        }
        public async Task<bool> IsTableInUseAsync(int tableId)
        {
            return await _context.ReservationTables
                .Include(rt => rt.Reservation)
                .AnyAsync(rt =>
                    rt.TableId == tableId &&
                    rt.Reservation.Status != "Cancelled");
        }
        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
