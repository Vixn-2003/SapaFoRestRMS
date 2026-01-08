using BusinessAccessLayer.DTOs.Shift;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.Services.Interfaces
{
    public interface IShiftService
    {
        Task<IEnumerable<ShiftViewDTO>> GetAllAsync();
        Task<ShiftViewDTO?> GetByIdAsync(int id);
        Task<ShiftViewDTO> CreateAsync(CreateShiftDTO dto);
        Task<ShiftViewDTO> UpdateAsync(int id, UpdateShiftDTO dto);
        Task<bool> DeleteAsync(int id);
    }
}
