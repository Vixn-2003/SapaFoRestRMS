using BusinessAccessLayer.DTOs.ShiftAssignment;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.Services.Interfaces
{
    public interface IShiftAssignmentService
    {
        Task<IEnumerable<ShiftAssignmentViewDTO>> GetAllAsync();
        Task<ShiftAssignmentViewDTO?> GetByIdAsync(int id);
        Task<ShiftAssignmentViewDTO> CreateAsync(CreateShiftAssignmentDTO dto);
        Task<ShiftAssignmentViewDTO> UpdateAsync(int id, UpdateShiftAssignmentDTO dto);
        Task<bool> DeleteAsync(int id);
    }

}
