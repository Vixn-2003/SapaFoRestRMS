using System.Threading;
using System.Threading.Tasks;
using BusinessAccessLayer.DTOs.Auth;

namespace BusinessAccessLayer.Services.Interfaces
{
    public interface IPasswordService
    {
        Task RequestResetAsync(RequestPasswordResetDto request, CancellationToken ct = default);
        Task<string> VerifyResetAsync(VerifyPasswordResetDto request, CancellationToken ct = default);
        Task ResetPasswordAsync(ResetPasswordDto request, CancellationToken ct = default);

        Task RequestChangeAsync(RequestChangePasswordDto request, CancellationToken ct = default);
        Task ChangeAsync(VerifyChangePasswordDto request, CancellationToken ct = default);
    }
}


