using BusinessAccessLayer.DTOs.Users;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BusinessAccessLayer.Services.Interfaces
{
    public interface IUserService
    {
        Task<IEnumerable<UserDto>> GetAllAsync(CancellationToken ct = default);
        Task<UserDto?> GetByIdAsync(int id, CancellationToken ct = default);
        Task<UserListResponse> SearchAsync(UserSearchRequest request, CancellationToken ct = default);
        Task<UserDto> CreateAsync(UserCreateRequest request, CancellationToken ct = default);
        Task UpdateAsync(int id, UserUpdateRequest request, CancellationToken ct = default);
        Task<UserDto> UpdateProfileAsync(int id, UserProfileUpdateRequest request, CancellationToken ct = default);
        Task DeleteAsync(int id, CancellationToken ct = default);
        Task ChangeStatusAsync(int id, int status, CancellationToken ct = default);
    }
}

