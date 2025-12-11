namespace WebSapaForestForStaff.DTOs.Customers
{
    public class CustomerVipListItemDto
    {
        public int CustomerId { get; set; }
        public string? FullName { get; set; }
        public string? Phone { get; set; }
        public int LoyaltyPoints { get; set; }
        public bool IsVip { get; set; }
    }
}

