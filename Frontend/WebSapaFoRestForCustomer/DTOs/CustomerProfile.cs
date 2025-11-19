namespace WebSapaFoRestForCustomer.DTOs
{
    public class CustomerProfile
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public int LoyaltyPoints { get; set; }
        public string? Notes { get; set; }
    }
}


