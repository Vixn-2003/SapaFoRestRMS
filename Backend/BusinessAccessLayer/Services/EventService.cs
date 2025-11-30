using BusinessAccessLayer.DTOs;
using BusinessAccessLayer.Services.Interfaces;
using DataAccessLayer.Repositories.Interfaces;
using DomainAccessLayer.Models;
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
        private readonly ICloudinaryService _cloudinaryService;

        public EventService(IEventRepository eventRepository, ICloudinaryService cloudinaryService)
        {
            _eventRepository = eventRepository;
            _cloudinaryService = cloudinaryService;
        }

        public async Task<List<EventDto>> GetTop6LatestEventsAsync()
        {
            var events = await _eventRepository.GetAllAsync();
            return events
                .Take(6)
                .Select(MapToDto)
                .ToList();
        }

        public async Task<(List<EventDto> Data, int TotalCount)> GetAllEventsAsync(
     string? search = null,
     int page = 1,
     int pageSize = 10)
        {
            var events = await _eventRepository.GetAllAsync();

            // Filter theo search
            if (!string.IsNullOrWhiteSpace(search))
            {
                events = events
                    .Where(e => e.Title.Contains(search, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            int totalCount = events.Count;

            // Phân trang
            events = events
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var data = events.Select(MapToDto).ToList();
            return (data, totalCount);
        }


        public async Task<EventDto?> GetByIdAsync(int id)
        {
            var ev = await _eventRepository.GetByIdAsync(id);
            if (ev == null) return null;
            return MapToDto(ev);
        }

        public async Task<EventDto> AddEventAsync(EventCreateDto dto)
        {
            // Validate logic
            if (dto.StartDate > dto.EndDate)
                throw new ArgumentException("Ngày bắt đầu phải nhỏ hơn hoặc bằng ngày kết thúc");

            string? imageUrl = null;
            if (dto.Image != null)
            {
                imageUrl = await _cloudinaryService.UploadImageAsync(dto.Image, "events");
            }

            var newEvent = new Event
            {
                Title = dto.Title,
                Description = dto.Description,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                Location = dto.Location,
                ImageUrl = imageUrl
            };

            await _eventRepository.AddAsync(newEvent);
            return MapToDto(newEvent);
        }

        public async Task<EventDto?> UpdateEventAsync(int id, EventUpdateDto dto)
        {
            var ev = await _eventRepository.GetByIdAsync(id);
            if (ev == null) return null;

            // Validate logic
            if (dto.StartDate > dto.EndDate)
                throw new ArgumentException("Ngày bắt đầu phải nhỏ hơn hoặc bằng ngày kết thúc");

            if (dto.Image != null)
            {
                if (!string.IsNullOrEmpty(ev.ImageUrl))
                {
                    await _cloudinaryService.DeleteImageAsync(ev.ImageUrl);
                }
                ev.ImageUrl = await _cloudinaryService.UploadImageAsync(dto.Image, "events");
            }

            ev.Title = dto.Title;
            ev.Description = dto.Description;
            ev.StartDate = dto.StartDate;
            ev.EndDate = dto.EndDate;
            ev.Location = dto.Location;

            await _eventRepository.UpdateAsync(ev);
            return MapToDto(ev);
        }


        public async Task<bool> DeleteEventAsync(int id)
        {
            var ev = await _eventRepository.GetByIdAsync(id);
            if (ev == null) return false;

            if (!string.IsNullOrEmpty(ev.ImageUrl))
            {
                await _cloudinaryService.DeleteImageAsync(ev.ImageUrl);
            }

            await _eventRepository.DeleteAsync(ev);
            return true;
        }

        private EventDto MapToDto(Event e)
        {
            return new EventDto
            {
                Id = e.EventId,
                Title = e.Title,
                Description = e.Description,
                StartDate = e.StartDate,
                EndDate = e.EndDate,
                Location = e.Location,
                ImageUrl = e.ImageUrl
            };
        }
    }
}
