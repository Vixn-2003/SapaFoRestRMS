namespace BusinessAccessLayer.DTOs.Users
{
    public class RoleDto
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; } = null!;
        public string Description { get; set; } = string.Empty; // Added for frontend compatibility
    }
}

