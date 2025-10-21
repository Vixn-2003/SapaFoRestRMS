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
      public async Task<(List<Reservation> Data, int TotalCount)> GetPendingAndConfirmedReservationsAsync(
    string? status = null,
    DateTime? date = null,
    string? customerName = null,
    string? phone = null,
    string? timeSlot = null,
    int page = 1,
    int pageSize = 10)
{
    var query = _context.Reservations
        .Include(r => r.Customer)
            .ThenInclude(c => c.User)
        .Include(r => r.ReservationTables)
        .AsQueryable();

    // Lọc trạng thái
    if (!string.IsNullOrEmpty(status))
        query = query.Where(r => r.Status == status);
    else
        query = query.Where(r => r.Status == "Pending" || r.Status == "Confirmed" || r.Status == "Cancelled");

    // Lọc theo ngày
    if (date.HasValue)
        query = query.Where(r => r.ReservationDate.Date == date.Value.Date);

    // Lọc theo ca (TimeSlot)
    if (!string.IsNullOrEmpty(timeSlot))
        query = query.Where(r => r.TimeSlot == timeSlot);

    // Lọc theo tên khách hàng
    if (!string.IsNullOrEmpty(customerName))
        query = query.Where(r => r.CustomerNameReservation.Contains(customerName));

    // Lọc theo số điện thoại khách hàng
    if (!string.IsNullOrEmpty(phone))
        query = query.Where(r => r.Customer.User.Phone.Contains(phone));

    // Tổng số bản ghi
    var totalCount = await query.CountAsync();

    // Phân trang
    var data = await query
        .OrderByDescending(r => r.ReservationDate)
        .ThenBy(r => r.ReservationTime)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

    return (data, totalCount);
}
        public async Task<object?> GetReservationDetailAsync(int reservationId)
        {
            var reservation = await _context.Reservations
                .Include(r => r.Customer)
                    .ThenInclude(c => c.User)
                .Include(r => r.ReservationTables)
                .ThenInclude(rt => rt.Table)
                .FirstOrDefaultAsync(r => r.ReservationId == reservationId);

            if (reservation == null) return null;

            return new
            {
                reservation.ReservationId,
                CustomerName = reservation.CustomerNameReservation,
                CustomerPhone = reservation.Customer.User.Phone,
                reservation.ReservationDate,
                reservation.ReservationTime,
                reservation.TimeSlot,
                reservation.NumberOfGuests,
                reservation.Status,
                reservation.Notes,
                reservation.DepositAmount,   
                reservation.DepositPaid,
                Tables = reservation.ReservationTables
            .Select(rt => new
            {
                rt.Table.TableId,
                rt.Table.TableNumber
            })
            .ToList()
            };
        }


        public async Task<List<Area>> GetAllAreasWithTablesAsync()
        {
            return await _context.Areas
                .Include(a => a.Tables.Where(t => t.Status == "Available"))
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
        public async Task<List<Reservation>> GetReservationsByPhoneAndDateAndSlotAsync(string phone, DateTime date, string slot)
        {
            return await _context.Reservations
                .Where(r => r.Customer.User.Phone == phone
                         && r.ReservationDate == date
                         && r.TimeSlot == slot)
                .ToListAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
