namespace WebSapaForestForStaff.DTOs.Auth
{
    public class LoginResponse
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public int RoleId { get; set; }
        public string RoleName { get; set; } = null!;
        public string Token { get; set; } = null!;
    }
}
