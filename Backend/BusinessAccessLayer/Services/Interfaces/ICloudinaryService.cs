using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace BusinessAccessLayer.Services.Interfaces
{
    public interface ICloudinaryService
    {
        Task<string?> UploadFileAsync(IFormFile file);
        Task<string?> UploadImageAsync(IFormFile file, string folder = "uploads");
        Task<bool> DeleteImageAsync(string imageUrl);
    }
}
