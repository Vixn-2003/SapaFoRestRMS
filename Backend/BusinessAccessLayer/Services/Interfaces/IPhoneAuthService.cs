using System.Threading;
using System.Threading.Tasks;
using BusinessAccessLayer.DTOs.Auth;

namespace BusinessAccessLayer.Services.Interfaces
{
    public interface IPhoneAuthService
    {
        Task RequestOtpAsync(string phone, CancellationToken ct = default);
        Task<LoginResponse> VerifyOtpAsync(string phone, string code, CancellationToken ct = default);
    }
}


