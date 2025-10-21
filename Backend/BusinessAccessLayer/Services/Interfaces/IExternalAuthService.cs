using System.Threading;
using System.Threading.Tasks;
using BusinessAccessLayer.DTOs.Auth;

namespace BusinessAccessLayer.Services.Interfaces
{
    public interface IExternalAuthService
    {
        Task<LoginResponse> GoogleLoginAsync(GoogleLoginRequest request, CancellationToken ct = default);
    }
}


