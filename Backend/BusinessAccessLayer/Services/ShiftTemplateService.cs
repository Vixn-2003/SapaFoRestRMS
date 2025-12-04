using BusinessAccessLayer.DTOs.ShiftTemplateDTOs;
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
    public class ShiftTemplateService : IShiftTemplateService
    {
        private readonly IShiftTemplateRepository _repo;

        public ShiftTemplateService(IShiftTemplateRepository repo)
        {
            _repo = repo;
        }

        public async Task<List<ShiftTemplateResponseDTO>> GetAllAsync()
        {
            var list = await _repo.GetAllAsync();

            return list.Select(x => new ShiftTemplateResponseDTO
            {
                Id = x.Id,
                Code = x.Code,
                StartTime = x.StartTime,
                EndTime = x.EndTime,
                RequiredEmployees = x.RequiredEmployees,
                DayTypeId = x.DayTypeId,
                DayTypeName = x.DayType.Name,
                DepartmentId = x.DepartmentId,
                DepartmentName = x.Department.Name
            }).ToList();
        }

        public async Task<ShiftTemplateResponseDTO?> GetByIdAsync(int id)
        {
            var x = await _repo.GetByIdAsync(id);
            if (x == null) return null;

            return new ShiftTemplateResponseDTO
            {
                Id = x.Id,
                Code = x.Code,
                StartTime = x.StartTime,
                EndTime = x.EndTime,
                RequiredEmployees = x.RequiredEmployees,
                DayTypeId = x.DayTypeId,
                DayTypeName = x.DayType.Name,
                DepartmentId = x.DepartmentId,
                DepartmentName = x.Department.Name
            };
        }

        public async Task<(bool Success, string Message)> CreateAsync(ShiftTemplateCreateDTO dto)
        {
            if (!await _repo.DayTypeExistsAsync(dto.DayTypeId))
                return (false, "DayTypeId không tồn tại.");

            if (!await _repo.DepartmentExistsAsync(dto.DepartmentId))
                return (false, "DepartmentId không tồn tại.");

            if (await _repo.CodeExistsAsync(dto.Code))
                return (false, "Mã ca (Code) đã tồn tại.");

            if (dto.StartTime >= dto.EndTime)
                return (false, "StartTime phải nhỏ hơn EndTime.");

            if (dto.RequiredEmployees <= 0)
                return (false, "RequiredEmployees phải > 0.");

            var entity = new ShiftTemplate
            {
                DayTypeId = dto.DayTypeId,
                DepartmentId = dto.DepartmentId,
                Code = dto.Code,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                RequiredEmployees = dto.RequiredEmployees
            };

            await _repo.AddAsync(entity);
            await _repo.SaveChangesAsync();

            return (true, "Tạo ShiftTemplate thành công.");
        }

        public async Task<(bool Success, string Message)> UpdateAsync(int id, ShiftTemplateUpdateDTO dto)
        {
            var entity = await _repo.GetByIdAsync(id);
            if (entity == null)
                return (false, "Không tìm thấy ShiftTemplate.");

            if (!await _repo.DayTypeExistsAsync(dto.DayTypeId))
                return (false, "DayTypeId không tồn tại.");

            if (!await _repo.DepartmentExistsAsync(dto.DepartmentId))
                return (false, "DepartmentId không tồn tại.");

            if (await _repo.CodeExistsAsync(dto.Code, id))
                return (false, "Mã ca (Code) đã được sử dụng.");

            if (dto.StartTime >= dto.EndTime)
                return (false, "StartTime phải nhỏ hơn EndTime.");

            if (dto.RequiredEmployees <= 0)
                return (false, "RequiredEmployees phải > 0.");

            entity.DayTypeId = dto.DayTypeId;
            entity.DepartmentId = dto.DepartmentId;
            entity.Code = dto.Code;
            entity.StartTime = dto.StartTime;
            entity.EndTime = dto.EndTime;
            entity.RequiredEmployees = dto.RequiredEmployees;

            _repo.Update(entity);
            await _repo.SaveChangesAsync();

            return (true, "Cập nhật ShiftTemplate thành công.");
        }

        public async Task<(bool Success, string Message)> DeleteAsync(int id)
        {
            var entity = await _repo.GetByIdAsync(id);
            if (entity == null)
                return (false, "Không tìm thấy ShiftTemplate.");

            _repo.Delete(entity);
            await _repo.SaveChangesAsync();

            return (true, "Xóa ShiftTemplate thành công.");
        }
    }
}
