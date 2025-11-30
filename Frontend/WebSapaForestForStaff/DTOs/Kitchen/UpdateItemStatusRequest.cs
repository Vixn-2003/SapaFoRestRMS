namespace WebSapaForestForStaff.DTOs.Kitchen
{
    /// <summary>
    /// Request to update item status from station screen
    /// </summary>
    public class UpdateItemStatusRequest
    {
        public int OrderDetailId { get; set; }
        public string NewStatus { get; set; } = string.Empty; // "Cooking" or "Done"
        public int UserId { get; set; } // Who pressed the button
    }
}

