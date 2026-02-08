using System.Threading;
using System.Threading.Tasks;
using BusinessAccessLayer.DTOs.UserManagement;

namespace BusinessAccessLayer.Services.Interfaces
{
    public interface IUserManagementService
    {
        Task<(int userId, string tempPassword)> CreateManagerAsync(CreateManagerRequest request, int adminUserId, CancellationToken ct = default);
        Task SendStaffVerificationCodeAsync(CreateStaffVerificationRequest request, int managerUserId, CancellationToken ct = default);
        Task<(int userId, int staffId, string tempPassword)> CreateStaffAsync(CreateStaffRequest request, int managerUserId, CancellationToken ct = default);
    }
}


