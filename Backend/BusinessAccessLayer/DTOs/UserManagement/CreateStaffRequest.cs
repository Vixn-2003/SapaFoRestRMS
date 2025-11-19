using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BusinessAccessLayer.DTOs.UserManagement
{
    public class CreateStaffRequest
    {
        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = null!;

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; } = null!;

        [Required]
        [StringLength(6, MinimumLength = 6)]
        [RegularExpression(@"^\d{6}$")]
        public string VerificationCode { get; set; } = null!;

        public DateOnly? HireDate { get; set; }

        public decimal? SalaryBase { get; set; }

        // Positions to assign to this staff (optional, can be empty)
        public List<int> PositionIds { get; set; } = new();

        // Optional explicit role id; if null, role will be resolved by name "Staff"
        public int? RoleId { get; set; }
    }
}


