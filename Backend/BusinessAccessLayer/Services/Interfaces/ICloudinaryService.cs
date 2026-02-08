using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace BusinessAccessLayer.Services.Interfaces
{
    public interface ICloudinaryService
    {
        Task<string?> UploadFileAsync(IFormFile file);
        Task<string?> UploadImageAsync(IFormFile file, string folder = "uploads");
        Task<string?> UploadPdfAsync(byte[] pdfBytes, string fileName, string folder = "receipts");
        Task<bool> DeleteImageAsync(string imageUrl);
    }
}
