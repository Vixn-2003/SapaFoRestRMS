using BusinessAccessLayer.DTOs;
using static BusinessAccessLayer.Services.OrderTableService;

namespace BusinessAccessLayer.Services.Interfaces
{
    public interface IOrderTableService
    {
        Task<IEnumerable<TableOrderDto>> GetTablesByReservationStatusAsync(string status);
    }
}
