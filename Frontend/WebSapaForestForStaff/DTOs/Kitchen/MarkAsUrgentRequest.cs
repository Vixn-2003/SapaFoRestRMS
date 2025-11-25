namespace WebSapaForestForStaff.DTOs.Kitchen
{
    /// <summary>
    /// Request to mark order detail as urgent/not urgent
    /// </summary>
    public class MarkAsUrgentRequest
    {
        public int OrderDetailId { get; set; }
        public bool IsUrgent { get; set; }
    }
}

