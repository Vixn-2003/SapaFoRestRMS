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
    public class ShiftsController : ControllerBase
    {
        private readonly SapaFoRestRmsContext _context;

        public ShiftsController(SapaFoRestRmsContext context)
        {
            _context = context;
        }

        // GET: api/Shifts
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ShiftDTO>>> GetShifts()
        {
            var shifts = await _context.Shifts
                .Include(s => s.Staff).ThenInclude(st => st.User)
                .Include(s => s.Department)
                .Include(s => s.Template)
                .ToListAsync();

            var dto = shifts.Select(s => new ShiftDTO
            {
                ShiftId = s.ShiftId,
                StaffId = s.StaffId,
                StaffName = s.Staff?.User?.FullName ?? "N/A",
                DepartmentId = s.DepartmentId,
                DepartmentName = s.Department?.Name ?? "N/A",
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                ShiftType = s.ShiftType,
                Note = s.Note,
                Status = s.Status
            }).ToList();

            return dto;
        }

        // POST: api/Shifts
        [HttpPost]
        public async Task<ActionResult<ShiftDTO>> CreateShift(ShiftCreateDTO dto)
        {
            var shift = new Shift
            {
                StaffId = dto.StaffId,
                DepartmentId = dto.DepartmentId,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                ShiftType = dto.ShiftType,
                Note = dto.Note,
                TemplateId = dto.TemplateId,
                RecurringId = dto.RecurringId,
                Status = 1
            };

            _context.Shifts.Add(shift);
            await _context.SaveChangesAsync();

            _context.ShiftHistorys.Add(new ShiftHistory
            {
                ShiftId = shift.ShiftId,
                Action = "Create",
                ActionAt = DateTime.Now,
                ActionBy = 1, // TODO: Lấy user đăng nhập
                Detail = "Created shift"
            });
            await _context.SaveChangesAsync();

            var result = new ShiftDTO
            {
                ShiftId = shift.ShiftId,
                StaffId = shift.StaffId,
                StaffName = shift.Staff.User.FullName,
                DepartmentId = shift.DepartmentId,
                DepartmentName = shift.Department.Name,
                StartTime = shift.StartTime,
                EndTime = shift.EndTime,
                ShiftType = shift.ShiftType,
                Note = shift.Note,
                Status = shift.Status
            };

            return CreatedAtAction(nameof(GetShifts), new { id = shift.ShiftId }, result);
        }

        // PUT: api/Shifts/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateShift(int id, ShiftUpdateDTO dto)
        {
            var shift = await _context.Shifts.FindAsync(id);
            if (shift == null) return NotFound();

            // Lưu các giá trị cũ để check trùng ca nếu cần
            int staffId = dto.StaffId ?? shift.StaffId;
            DateTime startTime = dto.StartTime ?? shift.StartTime;
            DateTime endTime = dto.EndTime ?? shift.EndTime;

            // Chỉ check trùng ca khi có thay đổi staff hoặc thời gian
            bool isChanged = (dto.StaffId.HasValue && dto.StaffId.Value != shift.StaffId) ||
                             (dto.StartTime.HasValue && dto.StartTime.Value != shift.StartTime) ||
                             (dto.EndTime.HasValue && dto.EndTime.Value != shift.EndTime);

            if (isChanged && await IsShiftOverlap(staffId, startTime, endTime, id))
                return BadRequest("Nhân viên đã có ca trùng giờ!");

            // Cập nhật các trường nếu có giá trị mới
            if (dto.StaffId.HasValue) shift.StaffId = dto.StaffId.Value;
            if (dto.DepartmentId.HasValue) shift.DepartmentId = dto.DepartmentId.Value;
            if (dto.StartTime.HasValue) shift.StartTime = dto.StartTime.Value;
            if (dto.EndTime.HasValue) shift.EndTime = dto.EndTime.Value;
            if (!string.IsNullOrEmpty(dto.ShiftType)) shift.ShiftType = dto.ShiftType;
            if (!string.IsNullOrEmpty(dto.Note)) shift.Note = dto.Note;

            await _context.SaveChangesAsync();

            // Lưu lịch sử cập nhật
            _context.ShiftHistorys.Add(new ShiftHistory
            {
                ShiftId = shift.ShiftId,
                Action = "Update",
                ActionAt = DateTime.Now,
                ActionBy = 0, // TODO: lấy user đăng nhập
                Detail = "Updated shift"
            });
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteShift(int id)
        {
            var shift = await _context.Shifts
                .Include(s => s.ShiftHistories) // Load các history liên quan
                .FirstOrDefaultAsync(s => s.ShiftId == id);

            if (shift == null) return NotFound();

            // Xóa các history trước
            if (shift.ShiftHistories.Any())
                _context.ShiftHistorys.RemoveRange(shift.ShiftHistories);

            _context.Shifts.Remove(shift);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPost("CreateWithRepeat")]
        public async Task<IActionResult> CreateWithRepeat([FromBody] ShiftCreateWithRepeatDTO dto)
        {
            if (dto.StaffIds == null || !dto.StaffIds.Any())
                return BadRequest("Chưa chọn nhân viên");

            // Lấy template
            var template = await _context.ShiftTemplates.FindAsync(dto.TemplateId);
            if (template == null)
                return BadRequest("Template không tồn tại");

            foreach (var staffId in dto.StaffIds)
            {
                // Lấy ngày bắt đầu / kết thúc, nếu null thì dùng giờ từ template
                DateTime curStart = dto.StartTime != default
                    ? dto.StartTime
                    : DateTime.Today + template.Start.ToTimeSpan();
                DateTime curEnd = dto.EndTime != default
                    ? dto.EndTime
                    : DateTime.Today + template.End.ToTimeSpan();

                int count = 1;
                if (dto.RepeatType == "daily")
                    count = dto.RepeatDays ?? 1;
                else if (dto.RepeatType == "weekly")
                    count = dto.RepeatWeeks ?? 1;

                for (int i = 0; i < count; i++)
                {
                    // Chỉ tạo ca nếu không trùng giờ
                    if (!await IsShiftOverlap(staffId, curStart, curEnd))
                    {
                        var shift = new Shift
                        {
                            StaffId = staffId,
                            DepartmentId = template.DepartmentId,   // gán từ template
                            TemplateId = template.ShiftTemplateId,
                            ShiftType = template.ShiftType,
                            Status = 1,
                            StartTime = curStart,
                            EndTime = curEnd
                        };

                        _context.Shifts.Add(shift);
                        await _context.SaveChangesAsync();
                    }

                    // Tăng ngày / tuần
                    if (dto.RepeatType == "daily")
                    {
                        curStart = curStart.AddDays(1);
                        curEnd = curEnd.AddDays(1);
                    }
                    else if (dto.RepeatType == "weekly")
                    {
                        curStart = curStart.AddDays(7);
                        curEnd = curEnd.AddDays(7);
                    }
                }
            }

            return Ok();
        }

        private async Task<bool> IsShiftOverlap(int staffId, DateTime start, DateTime end, int? ignoreShiftId = null)
        {
            return await _context.Shifts.AnyAsync(s =>
                s.StaffId == staffId &&
                (ignoreShiftId == null || s.ShiftId != ignoreShiftId) &&
                ((start >= s.StartTime && start < s.EndTime) ||
                 (end > s.StartTime && end <= s.EndTime) ||
                 (start <= s.StartTime && end >= s.EndTime))
            );
        }
    }
}
