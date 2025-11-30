using DomainAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories.Interfaces
{
    public interface ICounterStaffRepository
    {
        Task<(List<Reservation> Data, int TotalCount)> GetReservationsByStatusAsync(
       string status,
       string? customerName = null,
       string? phone = null,
       string? timeSlot = null,
       int page = 1,
       int pageSize = 10);

        Task<Reservation?> GetReservationByIdAsync(int id);

        Task UpdateAsync(Reservation reservation);

    }
}
