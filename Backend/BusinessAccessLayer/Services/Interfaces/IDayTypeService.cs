using BusinessAccessLayer.DTOs.DayTypeDTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.Services.Interfaces
{
    public interface IDayTypeService
    {
        Task<List<DayTypeResponseDTO>> GetAllAsync();
        Task<DayTypeResponseDTO?> GetByIdAsync(int id);
        Task<(bool Success, string Message)> CreateAsync(DayTypeCreateDTO dto);
        Task<(bool Success, string Message)> UpdateAsync(int id, DayTypeUpdateDTO dto);
        Task<(bool Success, string Message)> DeleteAsync(int id);
    }
}
