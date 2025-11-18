namespace BusinessAccessLayer.DTOs.Users
{
    public class UserDto
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
        public int? CreatedBy { get; set; }
        public DateTime? ModifiedAt { get; set; }
        public bool? IsDeleted { get; set; }
    }
}

