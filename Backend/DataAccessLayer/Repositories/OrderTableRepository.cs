using DataAccessLayer.Dbcontext;
using DataAccessLayer.Repositories.Interfaces;
using DomainAccessLayer.Models;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Repositories
{
    public class OrderTableRepository : Repository<Reservation>, IOrderTableRepository
    {
        private readonly SapaFoRestRmsContext _context;

        public OrderTableRepository(SapaFoRestRmsContext context) : base(context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Reservation>> GetReservationsByStatusAsync(string status)
        {
            return await _context.Reservations
                .Include(r => r.ReservationTables.Where(rt => rt.Reservation.Status == status))
                    .ThenInclude(rt => rt.Table)
                        .ThenInclude(t => t.Area)
                .Where(r => r.Status == status)
                .ToListAsync();
        }

        public async Task<(List<ReservationTable> Tables, int TotalCount)> GetPagedDistinctReservationTablesByStatusAsync(string status, int page, int pageSize)
        {
            var query = _context.ReservationTables
                .Include(rt => rt.Table)
                    .ThenInclude(t => t.Area)
                .Include(rt => rt.Reservation)
                .Where(rt => rt.Reservation.Status == status);

            // Lấy toàn bộ dữ liệu thỏa điều kiện 
            var allTables = await query.ToListAsync();

            // Group by TableId và chọn bản ghi đầu tiên
            var distinctTables = allTables
                .GroupBy(rt => rt.Table.TableId)
                .Select(g => g.First())
                .ToList();

            var totalCount = distinctTables.Count;

            // Phân trang trên bộ nhớ
            var pagedTables = distinctTables
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return (pagedTables, totalCount);
        }

        public async Task<Reservation?> GetReservationByIdAndStatusAsync(int reservationId, string status)
        {
            return await _context.Reservations
                .Include(r => r.ReservationTables)
                    .ThenInclude(rt => rt.Table)
                        .ThenInclude(t => t.Area)
                .FirstOrDefaultAsync(r => r.ReservationId == reservationId && r.Status == status);
        }

        public async Task<IEnumerable<MenuItem>> GetAvailableMenuWithCategoryAsync()
        {
            return await _context.MenuItems
                .Include(m => m.Category)
                .Where(m => m.IsAvailable == true)
                .ToListAsync();
        }


    }
}
