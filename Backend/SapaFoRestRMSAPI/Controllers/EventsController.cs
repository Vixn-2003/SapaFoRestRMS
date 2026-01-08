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

        [HttpGet("top6")]
        public async Task<ActionResult<List<EventDto>>> GetTop6()
        {
            var events = await _eventService.GetTop6LatestEventsAsync();
            return Ok(events);
        }

        [HttpGet]
        public async Task<ActionResult> GetAll(
      string? search,
      int page = 1,
      int pageSize = 10)
        {
            var (events, totalCount) = await _eventService.GetAllEventsAsync(search, page, pageSize);

            return Ok(new
            {
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                Data = events
            });
        }


        [HttpGet("{id}")]
        public async Task<ActionResult<EventDto>> GetDetail(int id)
        {
            var ev = await _eventService.GetByIdAsync(id);
            if (ev == null) return NotFound();
            return Ok(ev);
        }

        [HttpPost]
        public async Task<ActionResult<EventDto>> Add([FromForm] EventCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var ev = await _eventService.AddEventAsync(dto);
                return CreatedAtAction(nameof(GetDetail), new { id = ev.Title }, ev);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<EventDto>> Update(int id, [FromForm] EventUpdateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var ev = await _eventService.UpdateEventAsync(id, dto);
                if (ev == null) return NotFound();
                return Ok(ev);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }


        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            var success = await _eventService.DeleteEventAsync(id);
            if (!success) return NotFound();
            return NoContent();
        }
    }
}
