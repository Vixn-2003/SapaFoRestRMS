using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.DTOs.UserProfile
{
    public class UserProfileDto
    {
        public int? UserId { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; } = null!;
        public string? PasswordH { get; set; } = null!;
        public string? Phone { get; set; }
        public int? ProvinceId { get; set; }
        public int? DistrictId { get; set; }
        public int? WardId { get; set; }
        public string? AddressDetail { get; set; }
        public int Role { get; set; }
        public int Status { get; set; }
    }
}
