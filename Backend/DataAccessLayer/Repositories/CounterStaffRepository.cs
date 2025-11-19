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
    public class CounterStaffRepository : ICounterStaffRepository
    {
        public readonly SapaFoRestRmsContext _context;
        public CounterStaffRepository(SapaFoRestRmsContext context) 
        {
        _context = context;
        }

        public async Task<(List<Reservation> Data, int TotalCount)> GetReservationsByStatusAsync(
         string status,
         string? customerName = null,
         string? phone = null,
         string? timeSlot = null,
         int page = 1,
         int pageSize = 10)
        {
            var today = DateTime.Today;
            var now = DateTime.Now;

            var query = _context.Reservations
                .Include(r => r.Customer)
                    .ThenInclude(c => c.User)
                .Include(r => r.ReservationTables)
                .AsQueryable();

            // Chỉ lấy đơn hôm nay
            query = query.Where(r => r.ReservationDate.Date == today);

            // Filter theo status
            query = query.Where(r => r.Status.Trim() == status);

            // Filter khác
            if (!string.IsNullOrEmpty(timeSlot))
                query = query.Where(r => r.TimeSlot == timeSlot);

            if (!string.IsNullOrEmpty(customerName))
                query = query.Where(r => r.CustomerNameReservation.Contains(customerName));

            if (!string.IsNullOrEmpty(phone))
                query = query.Where(r => r.Customer.User.Phone.Contains(phone));

            var totalCount = await query.CountAsync();

            // Sắp xếp: giờ >= hiện tại lên đầu
            var data = await query
                .OrderBy(r => r.ReservationTime < now)
                .ThenBy(r => r.ReservationTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (data, totalCount);
        }

        public async Task<Reservation?> GetReservationByIdAsync(int id)
        {
            return await _context.Reservations
                .Include(r => r.Customer)
                    .ThenInclude(c => c.User)
                .Include(r => r.ReservationTables)
                .FirstOrDefaultAsync(r => r.ReservationId == id);
        }

        public async Task UpdateAsync(Reservation reservation)
        {
            _context.Reservations.Update(reservation);
            await _context.SaveChangesAsync();
        }

    }
}
