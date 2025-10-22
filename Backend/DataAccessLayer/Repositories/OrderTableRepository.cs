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


    }
}
