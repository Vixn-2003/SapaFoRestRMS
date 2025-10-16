using BusinessAccessLayer.DTOs;
using BusinessAccessLayer.Services.Interfaces;
using DataAccessLayer.Repositories.Interfaces;
using DomainAccessLayer.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BusinessAccessLayer.Services
{
    public class AreaService : IAreaService
    {
        private readonly IAreaRepository _repo;

        public AreaService(IAreaRepository repo)
        {
            _repo = repo;
        }

        public async Task<(IEnumerable<AreaDto> Areas, int TotalCount)> GetAllAsync(string? searchName, int? floor, int page, int pageSize)
        {
            var areas = await _repo.GetAllAsync(searchName, floor, page, pageSize);
            var total = await _repo.GetTotalCountAsync(searchName, floor);

            var result = areas.Select(a => new AreaDto
            {
                AreaId = a.AreaId,
                AreaName = a.AreaName,
                Floor = a.Floor,
                Description = a.Description
            });

            return (result, total);
        }

        public async Task<AreaDto?> GetByIdAsync(int id)
        {
            var area = await _repo.GetByIdAsync(id);
            if (area == null) return null;

            return new AreaDto
            {
                AreaId = area.AreaId,
                AreaName = area.AreaName,
                Floor = area.Floor,
                Description = area.Description
            };
        }

        public async Task<(bool Success, string Message)> CreateAsync(AreaDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.AreaName))
                return (false, "Tên khu vực không được để trống.");

            if (dto.Floor <= 0)
                return (false, "Số tầng phải lớn hơn 0.");

            if (await _repo.ExistsAsync(dto.AreaName, dto.Floor))
                return (false, "Khu vực đã tồn tại trong tầng này.");

            var area = new Area
            {
                AreaName = dto.AreaName.Trim(),
                Floor = dto.Floor,
                Description = dto.Description
            };

            await _repo.AddAsync(area);
            return (true, "Thêm khu vực thành công.");
        }

        public async Task<(bool Success, string Message)> UpdateAsync(AreaDto dto)
        {
            var existing = await _repo.GetByIdAsync(dto.AreaId);
            if (existing == null)
                return (false, "Không tìm thấy khu vực.");

            if (string.IsNullOrWhiteSpace(dto.AreaName))
                return (false, "Tên khu vực không được để trống.");

            if (dto.Floor <= 0)
                return (false, "Số tầng phải lớn hơn 0.");

            if (await _repo.ExistsAsync(dto.AreaName, dto.Floor, dto.AreaId))
                return (false, "Khu vực đã tồn tại trong tầng này.");

            existing.AreaName = dto.AreaName.Trim();
            existing.Floor = dto.Floor;
            existing.Description = dto.Description;

            await _repo.UpdateAsync(existing);
            return (true, "Cập nhật khu vực thành công.");
        }

        public async Task<(bool Success, string Message)> DeleteAsync(int id)
        {
            var area = await _repo.GetByIdAsync(id);
            if (area == null)
                return (false, "Không tìm thấy khu vực.");

            if (await _repo.HasTablesAsync(id))
                return (false, "Không thể xóa vì khu vực còn chứa bàn.");

            await _repo.DeleteAsync(area);
            return (true, "Xóa khu vực thành công.");
        }
    }
}
