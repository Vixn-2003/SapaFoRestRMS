using BusinessAccessLayer.DTOs;
using DataAccessLayer.Dbcontext;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;

namespace SapaFoRestRMSAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ShiftHistoriesController : ControllerBase
    {
        private readonly SapaFoRestRmsContext _context;
        public ShiftHistoriesController(SapaFoRestRmsContext context) => _context = context;

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ShiftHistoryDTO>>> GetAll()
        {
            var histories = await _context.ShiftHistorys
                .Include(h => h.Shift).ThenInclude(s => s.Staff).ThenInclude(s => s.User)
                .ToListAsync();

            var dto = histories.Select(h => new ShiftHistoryDTO
            {
                HistoryId = h.ShiftHistoryId,
                ShiftId = h.ShiftId,
                ShiftName = h.Shift.Staff.User.FullName + " - " + h.Shift.StartTime.ToString("yyyy-MM-dd HH:mm"),
                ActionBy = h.ActionBy,
                Action = h.Action,
                ActionAt = h.ActionAt,
                Detail = h.Detail
            }).ToList();

            return dto;
        }
    }
}
