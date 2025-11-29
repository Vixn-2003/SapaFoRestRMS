namespace WebSapaForestForStaff.DTOs.Inventory
{
    public class ConfirmAuditRequest
    {
        public string AuditId { get; set; }
        public string AuditStatus { get; set; }
        public int ConfirmerId { get; set; }
        public string ConfirmerName { get; set; }
        public string ConfirmerPhone { get; set; }
        public string ConfirmerPosition { get; set; }
        public DateTime ConfirmedAt { get; set; }
    }
}
