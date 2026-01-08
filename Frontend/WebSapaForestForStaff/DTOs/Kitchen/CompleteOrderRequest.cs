namespace WebSapaForestForStaff.DTOs.Kitchen
{
    /// <summary>
    /// Request to complete entire order (from Sous Chef)
    /// </summary>
    public class CompleteOrderRequest
    {
        public int OrderId { get; set; }
        public int SousChefUserId { get; set; }
    }
}

