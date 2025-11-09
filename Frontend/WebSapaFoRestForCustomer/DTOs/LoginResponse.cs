namespace WebSapaFoRestForCustomer.DTOs
{
    public class LoginResponse
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Token { get; set; } = null!;
        public string? RefreshToken { get; set; }
        public int RoleId { get; set; }
    }
}


