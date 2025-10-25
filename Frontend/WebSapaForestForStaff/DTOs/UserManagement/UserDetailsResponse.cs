using System.ComponentModel.DataAnnotations;

namespace WebSapaForestForStaff.DTOs.UserManagement
{
    public class UserDetailsResponse
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? Phone { get; set; }
        public int RoleId { get; set; }
        public string RoleName { get; set; } = null!;
        public int Status { get; set; }
        public string? AvatarUrl { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? ModifiedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public string? CreatedByName { get; set; }
        public string? ModifiedByName { get; set; }
        
        // Login history
        public List<LoginHistoryItem> LoginHistory { get; set; } = new();
        
        // Recent activities
        public List<ActivityItem> RecentActivities { get; set; } = new();
    }

    public class LoginHistoryItem
    {
        public DateTime LoginTime { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public bool IsSuccessful { get; set; }
    }

    public class ActivityItem
    {
        public DateTime ActivityTime { get; set; }
        public string ActivityType { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string? IpAddress { get; set; }
    }
}
