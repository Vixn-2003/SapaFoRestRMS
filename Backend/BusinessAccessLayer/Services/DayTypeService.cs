using BusinessAccessLayer.DTOs.DayTypeDTOs;
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
    public class DayTypeService : IDayTypeService
    {
        private readonly IDayTypeRepository _repo;

        public DayTypeService(IDayTypeRepository repo)
        {
            _repo = repo;
        }

        public async Task<List<DayTypeResponseDTO>> GetAllAsync()
        {
            var list = await _repo.GetAllAsync();
            return list.Select(x => new DayTypeResponseDTO
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description
            }).ToList();
        }

        public async Task<DayTypeResponseDTO?> GetByIdAsync(int id)
        {
            var entity = await _repo.GetByIdAsync(id);
            if (entity == null) return null;

            return new DayTypeResponseDTO
            {
                Id = entity.Id,
                Name = entity.Name,
                Description = entity.Description
            };
        }

        public async Task<(bool Success, string Message)> CreateAsync(DayTypeCreateDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return (false, "Name không được để trống.");

            if (await _repo.ExistsByNameAsync(dto.Name))
                return (false, "DayType với tên này đã tồn tại.");

            var entity = new DayType
            {
                Name = dto.Name.Trim(),
                Description = dto.Description
            };

            await _repo.AddAsync(entity);
            await _repo.SaveChangesAsync();

            return (true, "Tạo DayType thành công.");
        }

        public async Task<(bool Success, string Message)> UpdateAsync(int id, DayTypeUpdateDTO dto)
        {
            var entity = await _repo.GetByIdAsync(id);
            if (entity == null)
                return (false, "Không tìm thấy DayType.");

            if (string.IsNullOrWhiteSpace(dto.Name))
                return (false, "Name không được để trống.");

            if (entity.Name != dto.Name && await _repo.ExistsByNameAsync(dto.Name))
                return (false, "Tên này đã tồn tại, vui lòng chọn tên khác.");

            entity.Name = dto.Name.Trim();
            entity.Description = dto.Description;

            _repo.Update(entity);
            await _repo.SaveChangesAsync();

            return (true, "Cập nhật DayType thành công.");
        }

        public async Task<(bool Success, string Message)> DeleteAsync(int id)
        {
            var entity = await _repo.GetByIdAsync(id);
            if (entity == null)
                return (false, "Không tìm thấy DayType.");

            _repo.Delete(entity);
            await _repo.SaveChangesAsync();
            return (true, "Xóa DayType thành công.");
        }
    }
}
