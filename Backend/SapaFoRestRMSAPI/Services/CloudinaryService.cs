using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace SapaFoRestRMSAPI.Services
{
    public class CloudinaryService
    {
        private readonly Cloudinary _cloudinary;

        public CloudinaryService(IConfiguration config)
        {
            var cloudName = config["Cloudinary:CloudName"];
            var apiKey = config["Cloudinary:ApiKey"];
            var apiSecret = config["Cloudinary:ApiSecret"];
            var account = new Account(cloudName, apiKey, apiSecret);
            _cloudinary = new Cloudinary(account);
        }

        // Original method
        public async Task<string> UploadFileAsync(IFormFile file)
        {
            if (file == null || file.Length == 0) return null;

            using var stream = file.OpenReadStream();
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = "brand_banners"
            };

            var result = await _cloudinary.UploadAsync(uploadParams);
            return result.SecureUrl.ToString();
        }

        // New method with custom folder support
        public async Task<string> UploadImageAsync(IFormFile file, string folder = "uploads")
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

        // Delete image by URL
        public async Task<bool> DeleteImageAsync(string imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl)) return false;

            try
            {
                // Extract public ID from URL
                var uri = new Uri(imageUrl);
                var segments = uri.Segments;
                var publicIdWithExtension = string.Join("", segments.Skip(segments.Length - 2));
                var publicId = publicIdWithExtension.Replace(".jpg", "")
                    .Replace(".png", "")
                    .Replace(".jpeg", "")
                    .Replace("/", "");

                var deletionParams = new DeletionParams(publicId);
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