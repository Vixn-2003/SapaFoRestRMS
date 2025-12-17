using System;

namespace BusinessAccessLayer.DTOs.CustomerProfile
{
    public class CustomerProfileDto
    {
        public int CustomerId { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? AvatarUrl { get; set; }
        public decimal? LoyaltyPoints { get; set; }
        public string? VipLevel { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
