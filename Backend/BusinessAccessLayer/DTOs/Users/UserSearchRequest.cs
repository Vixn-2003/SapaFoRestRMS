namespace BusinessAccessLayer.DTOs.Users
{
    public class UserSearchRequest
    {
        public string? SearchTerm { get; set; }
        public int? RoleId { get; set; }
        public int? Status { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string SortBy { get; set; } = "FullName";
        public string SortOrder { get; set; } = "asc"; // asc or desc
    }
}

