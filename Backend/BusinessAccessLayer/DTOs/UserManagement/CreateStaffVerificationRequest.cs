using System.ComponentModel.DataAnnotations;

namespace BusinessAccessLayer.DTOs.UserManagement
{
    public class CreateStaffVerificationRequest
    {
        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = null!;

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; } = null!;
    }
}



