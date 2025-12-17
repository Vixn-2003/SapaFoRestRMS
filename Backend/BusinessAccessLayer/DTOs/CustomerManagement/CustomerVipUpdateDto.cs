using System.ComponentModel.DataAnnotations;

namespace BusinessAccessLayer.DTOs.CustomerManagement
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
        
        /// <summary>
        /// Lý do thay đổi VIP status (optional - để audit log)
        /// </summary>
        public string? Reason { get; set; }
        
        /// <summary>
        /// Manager có override system VIP criteria hay không
        /// </summary>
        public bool IsManualOverride { get; set; } = false;
    }
}

