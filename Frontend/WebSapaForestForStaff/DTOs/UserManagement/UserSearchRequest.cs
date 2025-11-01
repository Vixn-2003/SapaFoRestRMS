using System.ComponentModel.DataAnnotations;

namespace WebSapaForestForStaff.DTOs.UserManagement
{
    public class UserSearchRequest
    {
        [Display(Name = "Tìm kiếm")]
        public string? SearchTerm { get; set; }

        [Display(Name = "Vai trò")]
        public int? RoleId { get; set; }

        [Display(Name = "Trạng thái")]
        public int? Status { get; set; }

        [Display(Name = "Trang")]
        public int Page { get; set; } = 1;

        [Display(Name = "Số lượng mỗi trang")]
        public int PageSize { get; set; } = 10;

        [Display(Name = "Sắp xếp theo")]
        public string SortBy { get; set; } = "FullName";

        [Display(Name = "Thứ tự")]
        public string SortOrder { get; set; } = "asc";
    }
}
