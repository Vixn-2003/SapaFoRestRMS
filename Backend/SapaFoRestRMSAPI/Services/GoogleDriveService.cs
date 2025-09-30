using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;

namespace SapaFoRestRMSAPI.Services
{
    public class GoogleDriveService
    {
        private readonly DriveService _driveService;
        private readonly string _folderId = "1X0Xg480hM3qXW3sr6oVRoT3Ix2Pp75jB"; // folderId của bạn

        public GoogleDriveService()
        {
            // Lấy đường dẫn tuyệt đối tới file JSON
            var keyFilePath = Path.Combine(Directory.GetCurrentDirectory(), "keys", "sapaforestrms-81161b168f3c.json");

            var credential = GoogleCredential.FromFile(keyFilePath)
                .CreateScoped(DriveService.Scope.Drive);

            _driveService = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "SapaFoRestRMS",
            });
        }

        public async Task<string> UploadFileAsync(IFormFile file)
        {
            var fileMetadata = new Google.Apis.Drive.v3.Data.File()
            {
                Name = file.FileName,
                Parents = new List<string> { _folderId }
            };

            using var stream = file.OpenReadStream();
            var request = _driveService.Files.Create(fileMetadata, stream, file.ContentType);
            request.Fields = "id";
            await request.UploadAsync();

            var fileId = request.ResponseBody.Id;

            return $"https://drive.google.com/uc?id={fileId}";
        }
    }
}
