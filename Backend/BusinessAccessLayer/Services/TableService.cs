using BusinessAccessLayer.DTOs;
using BusinessAccessLayer.Services.Interfaces;
using DataAccessLayer.Repositories.Interfaces;
using DomainAccessLayer.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BusinessAccessLayer.Services
{
    public class TableService : ITableService
    {
        private readonly ITableRepository _repository;

        public TableService(ITableRepository repository)
        {
            _repository = repository;
        }

        public async Task<(IEnumerable<TableManageDto> Tables, int TotalCount)> GetTablesAsync(
            string? search,
            int? capacity,
            int? areaId,
            int page,
            int pageSize, string? status )
        {
            var tables = await _repository.GetAllAsync(search, capacity, areaId, page, pageSize,status);
            var totalCount = await _repository.GetCountAsync(search, capacity, areaId);

            var result = tables.Select(t => new TableManageDto
            {
                TableId = t.TableId,
                TableNumber = t.TableNumber,
                Capacity = t.Capacity,
                AreaId = t.AreaId,
                AreaName = t.Area.AreaName,
                Status = t.Status ?? "Available"
            });

            return (result, totalCount);
        }

        public async Task<TableManageDto?> GetByIdAsync(int id)
        {
            var table = await _repository.GetByIdAsync(id);
            if (table == null) return null;

            return new TableManageDto
            {
                TableId = table.TableId,
                TableNumber = table.TableNumber,
                Capacity = table.Capacity,
                AreaId = table.AreaId,
                AreaName = table.Area.AreaName,
                Status = table.Status ?? "Available"
            };
        }

        public async Task AddAsync(TableCreateDto dto)
        {
            if (dto.Capacity <= 0)
                throw new ArgumentException("Sức chứa phải lớn hơn 0.");
            if (string.IsNullOrWhiteSpace(dto.TableNumber))
                throw new ArgumentException("Tên bàn không được để trống.");

            var table = new Table
            {
                TableNumber = dto.TableNumber,
                Capacity = dto.Capacity,
                AreaId = dto.AreaId,
                Status = dto.Status
            };

            await _repository.AddAsync(table);
            await _repository.SaveAsync();
        }

        public async Task UpdateAsync(int id, TableUpdateDto dto)
        {
            var table = await _repository.GetByIdAsync(id);
            if (table == null)
                throw new KeyNotFoundException("Không tìm thấy bàn.");

            if (dto.Capacity <= 0)
                throw new ArgumentException("Sức chứa phải lớn hơn 0.");
            if (string.IsNullOrWhiteSpace(dto.TableNumber))
                throw new ArgumentException("Tên bàn không được để trống.");

            table.TableNumber = dto.TableNumber;
            table.Capacity = dto.Capacity;
            table.AreaId = dto.AreaId;
            table.Status = dto.Status;

            await _repository.UpdateAsync(table);
            await _repository.SaveAsync();
        }


        public async Task<(bool Success, string Message)> DeleteAsync(int id)
        {
            bool inUse = await _repository.IsTableInUseAsync(id);
            if (inUse)
                return (false, "Không thể xóa vì bàn đang được sử dụng trong đặt bàn.");

            await _repository.DeleteAsync(id);
            await _repository.SaveAsync();
            return (true, "Xóa bàn thành công.");
        }
    }
}
