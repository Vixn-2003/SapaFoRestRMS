using System.Net;

namespace WebSapaForestForStaff.DTOs.Payment
{
    /// <summary>
    /// Đại diện dữ liệu file hóa đơn trả về từ API
    /// </summary>
    public class ReceiptFileDto
    {
        public bool Success { get; set; }
        public byte[] FileBytes { get; set; } = Array.Empty<byte>();
        public string FileName { get; set; } = "receipt.pdf";
        public string? ContentType { get; set; }
        public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.InternalServerError;
        public string? ErrorMessage { get; set; }
    }
}

