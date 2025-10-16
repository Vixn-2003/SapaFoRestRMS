using System;
using System.Collections.Generic;

namespace BusinessAccessLayer.DTOs.UserManagement
{
    public class StaffProfileDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }

        // Optional (schema currently does not include these)
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }

        public string RoleName { get; set; } = string.Empty;
        public List<string> PositionNames { get; set; } = new();

        // Employee status (using User.Status for now)
        public int Status { get; set; }
    }

    public class StaffProfileUpdateDto
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }

        // Optional placeholders
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }

        public int Status { get; set; }
        public List<int>? PositionIds { get; set; }
    }
}


