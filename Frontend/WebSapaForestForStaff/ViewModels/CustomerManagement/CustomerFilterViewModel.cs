namespace WebSapaForestForStaff.ViewModels.CustomerManagement
{
    /// <summary>
    /// ViewModel for customer filter parameters
    /// </summary>
    public class CustomerFilterViewModel
    {
        public string? Keyword { get; set; }
        public bool? IsVip { get; set; }
        public decimal? MinSpending { get; set; }
        public decimal? MaxSpending { get; set; }
        public int? MinVisits { get; set; }
        public int? MaxVisits { get; set; }
        public string SortBy { get; set; } = "TotalSpending";
        public string SortDirection { get; set; } = "desc";
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}

