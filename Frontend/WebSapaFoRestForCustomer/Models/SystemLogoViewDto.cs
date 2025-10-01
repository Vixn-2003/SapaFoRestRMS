namespace WebSapaFoRestForCustomer.Models
{
    public class SystemLogoViewDto
    {
        public int LogoId { get; set; }
        public string LogoName { get; set; } = null!;
        public string? LogoUrl { get; set; }
        public string? Description { get; set; }
        public bool? IsActive { get; set; }
    }
}
