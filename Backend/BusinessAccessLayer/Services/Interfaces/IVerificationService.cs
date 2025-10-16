using System.Threading;
using System.Threading.Tasks;

namespace BusinessAccessLayer.Services.Interfaces
{
    public interface IVerificationService
    {
        Task<string> GenerateAndSendCodeAsync(int userId, string email, string purpose, int ttlMinutes, CancellationToken ct = default);
        Task<bool> VerifyCodeAsync(int userId, string purpose, string code, CancellationToken ct = default);
        Task InvalidateCodesAsync(int userId, string purpose, CancellationToken ct = default);
    }
}


