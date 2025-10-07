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
    public class ReservationRepository : IReservationRepository
    {
        private readonly SapaFoRestRmsContext _context;

        public ReservationRepository(SapaFoRestRmsContext context)
        {
            _context = context;
        }

        public async Task<Reservation> CreateAsync(Reservation reservation)
        {
            _context.Reservations.Add(reservation);
            await _context.SaveChangesAsync();
            return reservation;
        }
        public async Task<List<Reservation>> GetPendingAndConfirmedReservationsAsync()
        {
            return await _context.Reservations
                .Include(r => r.Customer)
                    .ThenInclude(c => c.User)
                .Where(r => r.Status == "Pending" || r.Status == "Confirmed")
                .Include(r => r.ReservationTables)
                .ToListAsync();
        }

        public async Task<List<Area>> GetAllAreasWithTablesAsync()
        {
            return await _context.Areas
                .Include(a => a.Tables)
                .ToListAsync();
        }

        public async Task<List<int>> GetBookedTableIdsAsync(DateTime reservationDate, string timeSlot)
        {
            return await _context.ReservationTables
                .Where(rt => rt.Reservation.ReservationDate.Date == reservationDate.Date
                          && rt.Reservation.TimeSlot == timeSlot
                          && rt.Reservation.Status != "Cancelled")
                .Select(rt => rt.TableId)
                .ToListAsync();
        }

        public async Task<Reservation?> GetReservationByIdAsync(int reservationId)
        {
            return await _context.Reservations
                .Include(r => r.ReservationTables)
                .FirstOrDefaultAsync(r => r.ReservationId == reservationId);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
