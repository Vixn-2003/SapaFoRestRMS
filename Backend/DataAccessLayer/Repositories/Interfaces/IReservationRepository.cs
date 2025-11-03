using DomainAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories.Interfaces
{
    public interface IReservationRepository
    {
        Task<Reservation> CreateAsync(Reservation reservation);
        Task<(List<Reservation> Data, int TotalCount)> GetPendingAndConfirmedReservationsAsync(
    string? status = null,
    DateTime? date = null,
    string? customerName = null,
    string? phone = null,
    string? timeSlot = null,
    int page = 1,
    int pageSize = 10);
        Task<object?> GetReservationDetailAsync(int reservationId);

        Task<List<Area>> GetAllAreasWithTablesAsync();
        Task<List<int>> GetBookedTableIdsAsync(DateTime reservationDate, string timeSlot);
        Task<Reservation?> GetReservationByIdAsync(int reservationId);
        Task<List<Reservation>> GetReservationsByPhoneAndDateAndSlotAsync(string phone, DateTime date, string slot);
        Task<List<Reservation>> GetReservationsByCustomerAsync(int customerId);

        Task SaveChangesAsync();
    }

}
