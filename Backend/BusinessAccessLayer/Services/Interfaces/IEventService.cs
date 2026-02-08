using BusinessAccessLayer.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.Services.Interfaces
{
    public interface IEventService
    {
        Task<List<EventDto>> GetTop6LatestEventsAsync();
        Task<(List<EventDto> Data, int TotalCount)> GetAllEventsAsync(
           string? search = null,
           int page = 1,
           int pageSize = 10);
        Task<EventDto?> GetByIdAsync(int id);
        Task<EventDto> AddEventAsync(EventCreateDto dto);
        Task<EventDto?> UpdateEventAsync(int id, EventUpdateDto dto);
        Task<bool> DeleteEventAsync(int id);
    }
}
