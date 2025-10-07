using BusinessAccessLayer.DTOs;
using BusinessAccessLayer.Services.Interfaces;
using DataAccessLayer.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.Services
{
    public class EventService : IEventService
    {
        private readonly IEventRepository _eventRepository;

        public EventService(IEventRepository eventRepository)
        {
            _eventRepository = eventRepository;
        }

        public async Task<List<EventDto>> GetTop6LatestEventsAsync()
        {
            var events = await _eventRepository.GetAllAsync();
            return events
                .Take(6)
                .Select(e => new EventDto
                {
                    Title = e.Title,
                    Description = e.Description,
                    StartDate = e.StartDate,
                    EndDate = e.EndDate,
                    Location = e.Location,
                    ImageUrl = e.ImageUrl
                }).ToList();
        }

        public async Task<List<EventDto>> GetAllEventsAsync()
        {
            var events = await _eventRepository.GetAllAsync();
            return events
                .Select(e => new EventDto
                {
                    Title = e.Title,
                    Description = e.Description,
                    StartDate = e.StartDate,
                    EndDate = e.EndDate,
                    Location = e.Location,
                    ImageUrl = e.ImageUrl
                }).ToList();
        }
    }
}
