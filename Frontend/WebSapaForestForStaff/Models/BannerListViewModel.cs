namespace WebSapaForestForStaff.Models
{
    public class BannerListViewModel
    {
        public List<BrandBannerViewModel> Banners { get; set; } = new();
        public List<string> Statuses { get; set; } = new();
        public string? CurrentStatus { get; set; }
        public string? CurrentTitle { get; set; }
        public int PageNumber { get; set; }
        public int TotalPages { get; set; }
        public int TotalItems { get; set; }
    }
}
