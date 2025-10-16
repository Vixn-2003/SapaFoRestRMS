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

        [Phone]
        [StringLength(20)]
        public string? Phone { get; set; }

        [Required]
        public DateOnly HireDate { get; set; }

        [Required]
        public decimal SalaryBase { get; set; }

        // Positions to assign to this staff
        public List<int> PositionIds { get; set; } = new();

        // Optional explicit role id; if null, role will be resolved by name "Staff"
        public int? RoleId { get; set; }
    }
}


