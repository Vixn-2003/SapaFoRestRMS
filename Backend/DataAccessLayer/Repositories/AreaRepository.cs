using DataAccessLayer.Dbcontext;
using DataAccessLayer.Repositories.Interfaces;
using DomainAccessLayer.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories
{
    public class AreaRepository : IAreaRepository
    {
        private readonly SapaFoRestRmsContext _context;

        public AreaRepository(SapaFoRestRmsContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Area>> GetAllAsync(string? searchName, int? floor, int page, int pageSize)
        {
            var query = _context.Areas.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchName))
                query = query.Where(a => a.AreaName.Contains(searchName));

            if (floor.HasValue)
                query = query.Where(a => a.Floor == floor.Value);

            return await query
                .OrderBy(a => a.Floor)
                .ThenBy(a => a.AreaName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetTotalCountAsync(string? searchName, int? floor)
        {
            var query = _context.Areas.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchName))
                query = query.Where(a => a.AreaName.Contains(searchName));

            if (floor.HasValue)
                query = query.Where(a => a.Floor == floor.Value);

            return await query.CountAsync();
        }

        public async Task<Area?> GetByIdAsync(int id)
        {
            return await _context.Areas.FindAsync(id);
        }

        public async Task AddAsync(Area area)
        {
            _context.Areas.Add(area);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Area area)
        {
            _context.Areas.Update(area);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Area area)
        {
            _context.Areas.Remove(area);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> ExistsAsync(string areaName, int floor, int? excludeId = null)
        {
            return await _context.Areas.AnyAsync(a =>
                a.AreaName == areaName &&
                a.Floor == floor &&
                (!excludeId.HasValue || a.AreaId != excludeId.Value));
        }

        public async Task<bool> HasTablesAsync(int areaId)
        {
            return await _context.Tables.AnyAsync(t => t.AreaId == areaId);
        }
    }
}
