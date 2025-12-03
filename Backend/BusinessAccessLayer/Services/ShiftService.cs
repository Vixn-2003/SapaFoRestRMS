using BusinessAccessLayer.DTOs.Shift;
using BusinessAccessLayer.Services.Interfaces;
using DataAccessLayer.Dbcontext;
using DataAccessLayer.Repositories.Interfaces;
using DomainAccessLayer.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.Services
{
    public class ShiftService : IShiftService
    {
        private readonly IShiftRepository _shiftRepo;
        private readonly SapaFoRestRmsContext _context;

        public ShiftService(IShiftRepository shiftRepo, SapaFoRestRmsContext context)
        {
            _shiftRepo = shiftRepo;
            _context = context;
        }

        public async Task<IEnumerable<ShiftViewDTO>> GetAllAsync()
        {
            var data = await _shiftRepo.GetAllAsync();
            return data.Select(x => new ShiftViewDTO
            {
                Id = x.Id,
                Date = x.Date,
                DepartmentId = x.DepartmentId,
                DepartmentName = x.Department.Name,
                Code = x.Code,
                StartTime = x.StartTime,
                EndTime = x.EndTime,
                RequiredEmployees = x.RequiredEmployees
            });
        }

        public async Task<ShiftViewDTO?> GetByIdAsync(int id)
        {
            var x = await _shiftRepo.GetByIdAsync(id);
            if (x == null) return null;

            return new ShiftViewDTO
            {
                Id = x.Id,
                Date = x.Date,
                DepartmentId = x.DepartmentId,
                DepartmentName = x.Department.Name,
                Code = x.Code,
                StartTime = x.StartTime,
                EndTime = x.EndTime,
                RequiredEmployees = x.RequiredEmployees
            };
        }

        public async Task<ShiftViewDTO> CreateAsync(CreateShiftDTO dto)
        {
            var template = await _context.ShiftTemplates
                .Include(t => t.Department)
                .FirstOrDefaultAsync(t => t.Id == dto.TemplateId);

            if (template == null)
                throw new Exception("Template không tồn tại");

            bool conflict = await _shiftRepo.IsConflictAsync(
                dto.DepartmentId, dto.Date, template.StartTime, template.EndTime);

            if (conflict)
                throw new Exception("Ca bị trùng thời gian");

            var shift = new Shift
            {
                Date = dto.Date,
                TemplateId = dto.TemplateId,
                DepartmentId = dto.DepartmentId,
                Code = template.Code,
                StartTime = template.StartTime,
                EndTime = template.EndTime,
                RequiredEmployees = template.RequiredEmployees
            };

            await _shiftRepo.AddAsync(shift);
            await _shiftRepo.SaveChangesAsync();

            return await GetByIdAsync(shift.Id);
        }

        public async Task<ShiftViewDTO> UpdateAsync(int id, UpdateShiftDTO dto)
        {
            var shift = await _shiftRepo.GetByIdAsync(id);
            if (shift == null)
                throw new Exception("Không tìm thấy shift");

            var template = await _context.ShiftTemplates
                .FirstOrDefaultAsync(t => t.Id == dto.TemplateId);

            if (template == null)
                throw new Exception("Template không tồn tại");

            bool conflict = await _shiftRepo.IsConflictAsync(
                dto.DepartmentId, dto.Date, template.StartTime, template.EndTime, id);

            if (conflict)
                throw new Exception("Ca cập nhật bị trùng thời gian");

            shift.Date = dto.Date;
            shift.TemplateId = dto.TemplateId;
            shift.DepartmentId = dto.DepartmentId;
            shift.Code = template.Code;
            shift.StartTime = template.StartTime;
            shift.EndTime = template.EndTime;
            shift.RequiredEmployees = template.RequiredEmployees;

            _shiftRepo.Update(shift);
            await _shiftRepo.SaveChangesAsync();

            return await GetByIdAsync(id);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var shift = await _shiftRepo.GetByIdAsync(id);
            if (shift == null)
                return false;

            _shiftRepo.Delete(shift);
            return await _shiftRepo.SaveChangesAsync();
        }
    }

}
