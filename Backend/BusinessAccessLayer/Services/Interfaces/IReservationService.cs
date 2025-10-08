using BusinessAccessLayer.DTOs;
using DomainAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.Services.Interfaces
{
    public interface IReservationService
    {
        Task<Reservation> CreateReservationAsync(ReservationCreateDto dto);
        Task<object> GetPendingAndConfirmedReservationsAsync(
    string? status = null,
    DateTime? date = null,
    string? customerName = null,
    string? phone = null,
    string? timeSlot = null,
    int page = 1,
    int pageSize = 10);
        Task<object?> GetReservationDetailAsync(int reservationId);

        Task<object> GetAllTablesGroupedByAreaAsync();
        Task<List<int>> GetBookedTableIdsAsync(DateTime date, string slot);
        Task<object> SuggestTablesByAreasAsync(DateTime date, string slot, int guests, int? currentReservationId = null);
        Task<object> AssignTablesAsync(AssignTableDto dto);
        Task<object> ResetTablesAsync(int reservationId);
    }
}
