namespace BusinessAccessLayer.DTOs.Waiter
{
    /// <summary>
    /// Request để yêu cầu làm gấp một món
    /// </summary>
    public class RequestUrgentDto
    {
        public int OrderDetailId { get; set; }
        public int WaiterUserId { get; set; } // ID của waiter yêu cầu
        public string Reason { get; set; } = string.Empty; // Lý do: khách vội, trẻ con đói, khách VIP...
    }

    /// <summary>
    /// Response sau khi yêu cầu làm gấp
    /// </summary>
    public class RequestUrgentResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}

