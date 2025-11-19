using System;
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

        public async Task<Position?> GetByIdAsync(int id)
        {
            return await _context.Positions.FindAsync(id);
        }

        public async Task<IEnumerable<Position>> GetAllAsync()
        {
            return await _context.Positions.ToListAsync();
        }

        public async Task AddAsync(Position entity)
        {
            await _context.Positions.AddAsync(entity);
        }

        public async Task UpdateAsync(Position entity)
        {
            _context.Positions.Update(entity);
        }

        public async Task DeleteAsync(int id)
        {
            var position = await GetByIdAsync(id);
            if (position != null)
            {
                _context.Positions.Remove(position);
            }
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task<List<Position>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken ct = default)
        {
            return await _context.Positions
                .Where(p => ids.Contains(p.PositionId))
                .ToListAsync(ct);
        }

        public async Task<bool> IsNameExistsAsync(string positionName, CancellationToken ct = default)
        {
            return await _context.Positions
                .AnyAsync(p => p.PositionName == positionName, ct);
        }

        public async Task<bool> IsNameExistsAsync(string positionName, int excludeId, CancellationToken ct = default)
        {
            return await _context.Positions
                .AnyAsync(p => p.PositionName == positionName && p.PositionId != excludeId, ct);
        }

        public async Task<List<Position>> SearchAsync(string? searchTerm, int? status, CancellationToken ct = default)
        {
            var query = _context.Positions.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var keyword = searchTerm.Trim();
                query = query.Where(p => 
                    p.PositionName.Contains(keyword) || 
                    (p.Description != null && p.Description.Contains(keyword)));
            }

            if (status.HasValue)
            {
                query = query.Where(p => p.Status == status.Value);
            }

            return await query
                .OrderBy(p => p.PositionName)
                .ToListAsync(ct);
        }

        public async Task<(List<Position> Items, int TotalCount)> SearchWithPaginationAsync(
            string? searchTerm, 
            int? status, 
            int page, 
            int pageSize, 
            CancellationToken ct = default)
        {
            var query = _context.Positions.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var keyword = searchTerm.Trim();
                query = query.Where(p => 
                    p.PositionName.Contains(keyword) || 
                    (p.Description != null && p.Description.Contains(keyword)));
            }

            if (status.HasValue)
            {
                query = query.Where(p => p.Status == status.Value);
            }

            var totalCount = await query.CountAsync(ct);

            var items = await query
                .OrderBy(p => p.PositionName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return (items, totalCount);
        }
    }
}


