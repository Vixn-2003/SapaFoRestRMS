using Microsoft.AspNetCore.Mvc.Rendering;

namespace WebSapaFoRestForStaff.DTOs.OrderTable
{
    public class PagedQrResultViewModel
    {
        public List<TableViewModel> Tables { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }

        // Giữ trạng thái lọc
        public string? SearchString { get; set; }
        public string? AreaName { get; set; }
        public int? Floor { get; set; }

        // Tùy chọn cho dropdown
        public SelectList? AreaNames { get; set; }
        public SelectList? Floors { get; set; }
    }
}
