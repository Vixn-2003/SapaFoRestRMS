using BusinessAccessLayer.DTOs;
using DataAccessLayer.Dbcontext;
using DomainAccessLayer.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;

namespace SapaFoRestRMSAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WeeklyRecurringShiftsController : ControllerBase
    {
        private readonly SapaFoRestRmsContext _context;
        public WeeklyRecurringShiftsController(SapaFoRestRmsContext context) => _context = context;

        [HttpGet]
        public async Task<ActionResult<IEnumerable<WeeklyRecurringShiftDTO>>> GetAll()
        {
            var recurring = await _context.WeeklyRecurringShifts
                .Include(w => w.Staff).ThenInclude(s => s.User)
                .Include(w => w.Template)
                .ToListAsync();

            var dto = recurring.Select(w => new WeeklyRecurringShiftDTO
            {
                RecurringId = w.WeeklyRecurringShiftId,
                StaffId = w.StaffId,
                StaffName = w.Staff.User.FullName,
                TemplateId = w.TemplateId,
                TemplateName = w.Template.Name,
                DaysOfWeek = w.DaysOfWeek,
                StartDate = w.StartDate,
                EndDate = w.EndDate
            }).ToList();

            return dto;
        }

        [HttpPost]
        public async Task<ActionResult<WeeklyRecurringShiftDTO>> CreateRecurringShift(WeeklyRecurringShiftCreateDTO dto)
        {
            var recurring = new WeeklyRecurringShift
            {
                StaffId = dto.StaffId,
                TemplateId = dto.TemplateId,
                DaysOfWeek = dto.DaysOfWeek,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate
            };

            _context.WeeklyRecurringShifts.Add(recurring);
            await _context.SaveChangesAsync();

            // Auto-create Shift instances
            var days = dto.DaysOfWeek.Split(',');
            var template = await _context.ShiftTemplates.FindAsync(dto.TemplateId);
            if (template != null)
            {
                var current = dto.StartDate;
                while (current <= dto.EndDate)
                {
                    if (days.Contains(current.DayOfWeek.ToString()))
                    {
                        _context.Shifts.Add(new Shift
                        {
                            StaffId = dto.StaffId,
                            TemplateId = template.ShiftTemplateId,
                            DepartmentId = template.DepartmentId,
                            StartTime = current.ToDateTime(template.Start),
                            EndTime = current.ToDateTime(template.End),
                            ShiftType = template.ShiftType,
                            RecurringId = recurring.WeeklyRecurringShiftId,
                            Status = 1
                        });
                    }
                    current = current.AddDays(1);
                }
                await _context.SaveChangesAsync();
            }

            var result = new WeeklyRecurringShiftDTO
            {
                RecurringId = recurring.WeeklyRecurringShiftId,
                StaffId = recurring.StaffId,
                StaffName = (await _context.Staffs.Include(s => s.User).FirstOrDefaultAsync(s => s.StaffId == dto.StaffId))?.User.FullName ?? "",
                TemplateId = recurring.TemplateId,
                TemplateName = template?.Name ?? "",
                DaysOfWeek = recurring.DaysOfWeek,
                StartDate = recurring.StartDate,
                EndDate = recurring.EndDate
            };

            return CreatedAtAction(nameof(GetAll), new { id = recurring.WeeklyRecurringShiftId }, result);
        }
    }
}
