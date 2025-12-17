namespace WebSapaForestForStaff.DTOs.CustomerManagement
{
    /// <summary>
    /// DTO cho filter và search trong UC145 - View List Customer
    /// </summary>
    public class CustomerFilterDto
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? SearchKeyword { get; set; }
        public bool? IsVipOnly { get; set; }
        public decimal? MinSpending { get; set; }
        public decimal? MaxSpending { get; set; }
        public int? MinVisits { get; set; }
        public int? MaxVisits { get; set; }
        public string SortBy { get; set; } = "TotalSpending";
        public string SortDirection { get; set; } = "desc";
    }
}

