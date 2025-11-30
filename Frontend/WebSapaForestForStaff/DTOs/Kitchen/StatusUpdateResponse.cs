namespace WebSapaForestForStaff.DTOs.Kitchen
{
    /// <summary>
    /// Response after status update
    /// </summary>
    public class StatusUpdateResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public KitchenOrderItemDto? UpdatedItem { get; set; }
    }
}

