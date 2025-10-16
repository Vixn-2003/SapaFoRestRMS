using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessAccessLayer.DTOs.UserManagement;

namespace BusinessAccessLayer.Services.Interfaces
{
    public interface IStaffProfileService
    {
        Task<List<StaffProfileDto>> GetAllAsync(CancellationToken ct = default);
        Task<StaffProfileDto?> GetAsync(int userId, CancellationToken ct = default);
        Task UpdateAsync(int userId, StaffProfileUpdateDto update, CancellationToken ct = default);
    }
}


