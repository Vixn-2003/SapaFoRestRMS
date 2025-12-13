namespace BusinessAccessLayer.DTOs.CustomerManagement
{
    /// <summary>
    /// DTO cho filter và search trong UC145 - View List Customer
    /// </summary>
    public class CustomerFilterDto
    {
        /// <summary>
        /// Trang hiện tại (pagination)
        /// </summary>
        public int Page { get; set; } = 1;
        
        /// <summary>
        /// Số lượng items trên 1 trang
        /// </summary>
        public int PageSize { get; set; } = 20;
        
        /// <summary>
        /// Tìm kiếm theo tên hoặc số điện thoại
        /// </summary>
        public string? SearchKeyword { get; set; }
        
        /// <summary>
        /// Filter chỉ VIP customers
        /// </summary>
        public bool? IsVipOnly { get; set; }
        
        /// <summary>
        /// Filter theo total spending tối thiểu
        /// </summary>
        public decimal? MinSpending { get; set; }
        
        /// <summary>
        /// Filter theo total spending tối đa
        /// </summary>
        public decimal? MaxSpending { get; set; }
        
        /// <summary>
        /// Filter theo số lần visit tối thiểu
        /// </summary>
        public int? MinVisits { get; set; }
        
        /// <summary>
        /// Filter theo số lần visit tối đa
        /// </summary>
        public int? MaxVisits { get; set; }
        
        /// <summary>
        /// Sắp xếp theo field nào (e.g., "TotalSpending", "FullName", "LastVisit", "TotalVisits")
        /// </summary>
        public string SortBy { get; set; } = "TotalSpending";
        
        /// <summary>
        /// Hướng sắp xếp: "asc" hoặc "desc"
        /// </summary>
        public string SortDirection { get; set; } = "desc";
    }
}

