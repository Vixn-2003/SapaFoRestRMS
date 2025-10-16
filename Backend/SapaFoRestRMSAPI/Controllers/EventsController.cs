using BusinessAccessLayer.DTOs;
using BusinessAccessLayer.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace SapaFoRestRMSAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EventsController : ControllerBase
    {
        private readonly IEventService _eventService;

        public EventsController(IEventService eventService)
        {
            _eventService = eventService;
        }

        // GET api/events/top6
        [HttpGet("top6")]
        public async Task<ActionResult<List<EventDto>>> GetTop6()
        {
            var events = await _eventService.GetTop6LatestEventsAsync();
            return Ok(events);
        }

        // GET api/events
        [HttpGet]
        public async Task<ActionResult<List<EventDto>>> GetAll()
        {
            var events = await _eventService.GetAllEventsAsync();
            return Ok(events);
        }
    }
}
