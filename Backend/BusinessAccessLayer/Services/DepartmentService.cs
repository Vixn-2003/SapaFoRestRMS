using BusinessAccessLayer.DTOs.Department;
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
    public class DepartmentService : IDepartmentService
    {
        private readonly IDepartmentRepository _repo;

        public DepartmentService(IDepartmentRepository repo)
        {
            _repo = repo;
        }

        public async Task<List<DepartmentDTO>> GetAllAsync()
        {
            var items = await _repo.GetAllAsync();
            return items.Select(d => new DepartmentDTO
            {
                DepartmentId = d.DepartmentId,
                Name = d.Name,
                Status = d.Status
            }).ToList();
        }

        public async Task<DepartmentDTO?> GetByIdAsync(int id)
        {
            var d = await _repo.GetByIdAsync(id);
            if (d == null) return null;

            return new DepartmentDTO
            {
                DepartmentId = d.DepartmentId,
                Name = d.Name,
                Status = d.Status
            };
        }

        public async Task<string?> CreateAsync(DepartmentCreateDTO dto)
        {
            if (await _repo.ExistsNameAsync(dto.Name))
                return "Tên phòng ban đã tồn tại.";

            var department = new Department
            {
                Name = dto.Name,
                Status = dto.Status
            };

            await _repo.AddAsync(department);
            await _repo.SaveChangesAsync();
            return null;
        }

        public async Task<string?> UpdateAsync(int id, DepartmentUpdateDTO dto)
        {
            var d = await _repo.GetByIdAsync(id);
            if (d == null) return "Không tìm thấy phòng ban.";

            if (await _repo.ExistsNameAsync(dto.Name, id))
                return "Tên phòng ban đã tồn tại.";

            d.Name = dto.Name;
            d.Status = dto.Status;

            await _repo.UpdateAsync(d);
            await _repo.SaveChangesAsync();

            return null;
        }

        public async Task<string?> DeleteAsync(int id)
        {
            var d = await _repo.GetByIdAsync(id);
            if (d == null) return "Không tìm thấy phòng ban.";

            await _repo.DeleteAsync(d);
            await _repo.SaveChangesAsync();

            return null;
        }
    }
}
