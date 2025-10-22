using BusinessAccessLayer.DTOs;
using DomainAccessLayer.Models;
using static BusinessAccessLayer.Services.OrderTableService;

namespace BusinessAccessLayer.Services.Interfaces
{
    public interface IOrderTableService
    {
        Task<IEnumerable<TableOrderDto>> GetTablesByReservationStatusAsync(string status);

        Task<PagedTableOrderResult> GetTablesByReservationStatusAsync(string status, int page, int pageSize);

        Task<IEnumerable<TableOrderDto>> GetTablesByReservationIdAndStatusAsync(int reservationId, string status);

        Task<IEnumerable<MenuItemDto>> GetMenuForReservationAsync(int reservationId, string status);

    }
}
