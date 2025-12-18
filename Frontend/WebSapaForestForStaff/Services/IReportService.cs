namespace WebSapaForestForStaff.Services
{
    public interface IReportService
    {
        Task<byte[]> GenerateAuditReportPdfAsync(AuditReportRequest request);
    }

    public class AuditReportRequest
    {
        public DateTime DateFrom { get; set; }
        public DateTime DateTo { get; set; }
        public string? Status { get; set; }
        public string ReporterName { get; set; } = string.Empty;
        public string ReporterPosition { get; set; } = string.Empty;
    }
}
