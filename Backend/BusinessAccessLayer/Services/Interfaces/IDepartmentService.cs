using BusinessAccessLayer.DTOs.Department;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.Services.Interfaces
{
    public interface IDepartmentService
    {
        Task<List<DepartmentDTO>> GetAllAsync();
        Task<DepartmentDTO?> GetByIdAsync(int id);
        Task<string?> CreateAsync(DepartmentCreateDTO dto);
        Task<string?> UpdateAsync(int id, DepartmentUpdateDTO dto);
        Task<string?> DeleteAsync(int id);
    }
}
