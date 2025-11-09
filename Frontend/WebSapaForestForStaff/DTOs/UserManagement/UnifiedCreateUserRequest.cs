namespace WebSapaForestForStaff.DTOs.UserManagement
{
    public class UnifiedCreateUserRequest
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public int RoleId { get; set; }

        // Staff-specific
        public DateOnly? HireDate { get; set; }
        public decimal? SalaryBase { get; set; }
        public List<int> PositionIds { get; set; } = new();
    }
}
