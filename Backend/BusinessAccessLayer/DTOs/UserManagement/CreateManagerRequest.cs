using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BusinessAccessLayer.DTOs.UserManagement
{
    public class CreateManagerRequest
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

        // Optional explicit role id; if null, role will be resolved by name "Manager"
        public int? RoleId { get; set; }
    }
}


