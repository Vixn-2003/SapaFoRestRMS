namespace WebSapaForestForStaff.DTOs.Kitchen
{
    /// <summary>
    /// Request to recall (khôi phục) an order detail that was marked as Done
    /// </summary>
    public class RecallOrderDetailRequest
    {
        public int OrderDetailId { get; set; }
        public int UserId { get; set; }
    }
}

