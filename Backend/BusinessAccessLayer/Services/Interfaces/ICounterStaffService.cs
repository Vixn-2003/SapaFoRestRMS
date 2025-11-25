using BusinessAccessLayer.DTOs;
using DomainAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.Services.Interfaces
{
    public interface ICounterStaffService
    {
        Task<object> GetConfirmedReservationsAsync(
       string? customerName = null,
       string? phone = null,
       string? timeSlot = null,
       int page = 1,
       int pageSize = 10);

        Task<object> GetGuestSeatedReservationsAsync(
            string? customerName = null,
            string? phone = null,
            string? timeSlot = null,
            int page = 1,
            int pageSize = 10);

        Task<ReservationStatusDto?> UpdateReservationStatusAsync(int reservationId, string newStatus);
    }
}
