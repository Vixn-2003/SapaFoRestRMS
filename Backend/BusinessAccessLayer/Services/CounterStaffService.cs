using BusinessAccessLayer.DTOs;
using BusinessAccessLayer.Services.Interfaces;
using DataAccessLayer.Repositories;
using DataAccessLayer.Repositories.Interfaces;
using DomainAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.Services
{
    public class CounterStaffService : ICounterStaffService
    {
        public readonly ICounterStaffRepository _counterStaffRepository;

        public CounterStaffService(ICounterStaffRepository counterStaffRepository)
        {
            _counterStaffRepository = counterStaffRepository;
        }
        public async Task<object> GetConfirmedReservationsAsync(
      string? customerName = null,
      string? phone = null,
      string? timeSlot = null,
        int page = 1,
      int pageSize = 10)
        {
            var (reservations, totalCount) = await _counterStaffRepository
                .GetReservationsByStatusAsync("Confirmed", customerName, phone, timeSlot, page, pageSize);

            return FormatResult(reservations, totalCount, page, pageSize);
        }

        public async Task<object> GetGuestSeatedReservationsAsync(
            string? customerName = null,
            string? phone = null,
            string? timeSlot = null,
        int page = 1,
            int pageSize = 10)
        {
            var (reservations, totalCount) = await _counterStaffRepository
                .GetReservationsByStatusAsync("Guest Seated", customerName, phone, timeSlot, page, pageSize);

            return FormatResult(reservations, totalCount, page, pageSize);
        }

        public async Task<ReservationStatusDto?> UpdateReservationStatusAsync(int reservationId, string newStatus)
        {
            var reservation = await _counterStaffRepository.GetReservationByIdAsync(reservationId);
            if (reservation == null) return null;

            reservation.Status = newStatus;
            await _counterStaffRepository.UpdateAsync(reservation);

            return new ReservationStatusDto
            {
                ReservationId = reservation.ReservationId,
                Status = reservation.Status
            };
        }


        private object FormatResult(List<Reservation> reservations, int totalCount, int page, int pageSize)
        {
            return new
            {
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                Data = reservations.Select(r => new
                {
                    r.ReservationId,
                    CustomerName = r.CustomerNameReservation,
                    CustomerPhone = r.Customer?.User?.Phone,
                    r.ReservationDate,
                    r.ReservationTime,
                    r.TimeSlot,
                    r.NumberOfGuests,
                    r.Status,
                    r.DepositAmount,
                    r.DepositPaid,
                    TableIds = r.ReservationTables.Select(rt => rt.TableId).ToList()
                }).ToList()
            };
        }

    }
}
