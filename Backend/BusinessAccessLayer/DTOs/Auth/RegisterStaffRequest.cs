using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.DTOs.Auth
{
    public sealed class RegisterStaffRequest
    {
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? Phone { get; set; }

        // Staff-specific
        public string Position { get; set; } = null!;
        public DateOnly HireDate { get; set; }
        public decimal SalaryBase { get; set; }

        // Optional overrides
        public int? UserStatus { get; set; }  // default 0
        public int? StaffStatus { get; set; } // default 0

        // If you prefer explicit role, pass it (e.g., staff role id)
        public int? RoleId { get; set; }      // if null, will lookup RoleName = "Staff"
    }
}
