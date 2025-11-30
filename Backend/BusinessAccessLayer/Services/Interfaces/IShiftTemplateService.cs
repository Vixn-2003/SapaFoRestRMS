using BusinessAccessLayer.DTOs.ShiftTemplateDTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.Services.Interfaces
{
    public interface IShiftTemplateService
    {
        Task<List<ShiftTemplateResponseDTO>> GetAllAsync();
        Task<ShiftTemplateResponseDTO?> GetByIdAsync(int id);
        Task<(bool Success, string Message)> CreateAsync(ShiftTemplateCreateDTO dto);
        Task<(bool Success, string Message)> UpdateAsync(int id, ShiftTemplateUpdateDTO dto);
        Task<(bool Success, string Message)> DeleteAsync(int id);
    }
}
