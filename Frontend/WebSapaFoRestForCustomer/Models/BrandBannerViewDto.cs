namespace WebSapaFoRestForCustomer.Models
{
    public class BrandBannerViewDto
    {
        public int BannerId { get; set; }
        public string Title { get; set; }
        public string? ImageUrl { get; set; }
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public string? Status { get; set; }
    }
}
