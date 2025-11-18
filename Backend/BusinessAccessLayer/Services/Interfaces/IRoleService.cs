using BusinessAccessLayer.DTOs.Users;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BusinessAccessLayer.Services.Interfaces
{
    public interface IRoleService
    {
        Task<List<RoleDto>> GetAllAsync(CancellationToken ct = default);
        Task<RoleDto?> GetByIdAsync(int id, CancellationToken ct = default);
    }
}

