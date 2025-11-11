using DataAccessLayer.Dbcontext;
using DataAccessLayer.Repositories.Interfaces;
using DomainAccessLayer.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories
{
    public class DashboardTableRepository : IDashboardTableRepository
    {
        private readonly SapaFoRestRmsContext _context;

        public DashboardTableRepository(SapaFoRestRmsContext context)
        {
            _context = context;
        }

        public async Task<List<(Table Table, Reservation ActiveReservation)>> GetFilteredTablesWithStatusAsync(string? areaName, int? floor, string? searchString)
        {
            var query = _context.Tables.Include(t => t.Area).Include(t => t.ReservationTables)
                .ThenInclude(t => t.Reservation)
                .AsQueryable();

            if (floor.HasValue)
            {
                query = query.Where(t => t.Area.Floor == floor.Value);
            }
            if (!string.IsNullOrEmpty(areaName))
            {
                query = query.Where(t => t.Area.AreaName == areaName);
            }

            // (MỚI) Thêm logic tìm kiếm theo tên/số bàn
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(t => t.TableNumber.Contains(searchString));
            }

            var projectedResult = await query
                .OrderBy(t => t.TableNumber)
                .Select(t => new {
                    Table = t,
                    ActiveReservation = t.ReservationTables
                        .Select(rt => rt.Reservation)
                        .Where(r => r.Status == "Guest Seated" || r.Status == "Confirmed")
                        .OrderByDescending(r => r.ReservationTime)
                        .FirstOrDefault()
                })
                .ToListAsync();

            var result = projectedResult.Select(data =>
                (data.Table, data.ActiveReservation)
            ).ToList();

            return result;
        }
    }
}