using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using BusinessAccessLayer.Services.Interfaces; // 👈 cần dòng này

namespace BusinessAccessLayer.Services
{
    public class CloudinaryService : ICloudinaryService // 👈 phải có interface ở đây
    {
        private readonly Cloudinary _cloudinary;

        public CloudinaryService(IConfiguration config)
        {
            var cloudName = config["CloudinarySettings:CloudName"];
            var apiKey = config["CloudinarySettings:ApiKey"];
            var apiSecret = config["CloudinarySettings:ApiSecret"];

            var account = new Account(cloudName, apiKey, apiSecret);
            _cloudinary = new Cloudinary(account);
        }

        public async Task<string?> UploadFileAsync(IFormFile file)
        {
            if (file == null || file.Length == 0) return null;

            using var stream = file.OpenReadStream();
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = "brand_banners"
            };

            var result = await _cloudinary.UploadAsync(uploadParams);
            return result?.SecureUrl?.ToString();
        }

        public async Task<string?> UploadImageAsync(IFormFile file, string folder = "uploads")
        {
            if (file == null || file.Length == 0) return null;

            using var stream = file.OpenReadStream();
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = folder,
                Transformation = new Transformation()
                    .Width(1200)
                    .Height(630)
                    .Crop("limit")
                    .Quality("auto")
            };

            var result = await _cloudinary.UploadAsync(uploadParams);
            return result?.SecureUrl?.ToString();
        }

        /// <summary>
        /// Upload PDF file to Cloudinary
        /// </summary>
        /// <param name="pdfBytes">PDF file as byte array</param>
        /// <param name="fileName">File name (e.g., "RMS000123.pdf")</param>
        /// <param name="folder">Cloudinary folder path (default: "receipts")</param>
        /// <returns>Secure URL of uploaded PDF, or null if upload fails</returns>
        public async Task<string?> UploadPdfAsync(byte[] pdfBytes, string fileName, string folder = "receipts")
        {
            if (pdfBytes == null || pdfBytes.Length == 0)
            {
                return null;
            }

            try
            {
                using var stream = new MemoryStream(pdfBytes);
                var uploadParams = new RawUploadParams
                {
                    File = new FileDescription(fileName, stream),
                    Folder = folder,
                    PublicId = Path.GetFileNameWithoutExtension(fileName) // Remove .pdf extension for public ID
                };

                // RawUploadParams automatically uses ResourceType.Raw
                var result = await _cloudinary.UploadAsync(uploadParams);
                return result?.SecureUrl?.ToString();
            }
            catch (Exception)
            {
                // Log error but don't throw - return null to allow fallback to local storage
                return null;
            }
        }

        public async Task<bool> DeleteImageAsync(string imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl)) return false;

            try
            {
                var uri = new Uri(imageUrl);
                var segments = uri.Segments;
                var publicIdWithExtension = string.Join("", segments.Skip(segments.Length - 2));
                var publicId = publicIdWithExtension
                    .Replace(".jpg", "")
                    .Replace(".png", "")
                    .Replace(".jpeg", "")
                    .Replace(".pdf", "") // Add PDF support
                    .Replace("/", "");

                var deletionParams = new DeletionParams(publicId)
                {
                    ResourceType = ResourceType.Raw // For PDF files
                };
                var result = await _cloudinary.DestroyAsync(deletionParams);

                return result.Result == "ok";
            }
            catch
            {
                return false;
            }
        }
    }
}
