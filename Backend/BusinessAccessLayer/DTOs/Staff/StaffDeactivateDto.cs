using System.ComponentModel.DataAnnotations;

namespace BusinessAccessLayer.DTOs.Staff
{
    /// <summary>
    /// DTO for deactivating staff
    /// Used in UC57 - Deactivate / Delete Staff
    /// </summary>
    public class StaffDeactivateDto
    {
        [Required]
        public int StaffId { get; set; }

        /// <summary>
        /// Reason for deactivation
        /// </summary>
        [StringLength(500, ErrorMessage = "Reason must not exceed 500 characters")]
        public string? Reason { get; set; }
    }
}

