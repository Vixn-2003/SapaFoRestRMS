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
        Task<List<EventDto>> GetAllEventsAsync();
    }
}
