using System.ComponentModel.DataAnnotations;

namespace WebSapaForestForStaff.DTOs.CustomerManagement
{
    /// <summary>
    /// DTO cho UC147 - Update VIP Status
    /// </summary>
    public class CustomerVipUpdateDto
    {
        [Required]
        public int CustomerId { get; set; }
        
        [Required]
        public bool IsVip { get; set; }
        
        public string? Reason { get; set; }
        public bool IsManualOverride { get; set; } = false;
    }
}

