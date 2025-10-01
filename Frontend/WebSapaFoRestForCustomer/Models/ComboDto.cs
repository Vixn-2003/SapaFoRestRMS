namespace WebSapaFoRestForCustomer.Models
{
    public class ComboDto
    {
        public string Name { get; set; } = null!;
        public string? ImageUrl { get; set; }
        public string? Description { get; set; }
        public decimal Price { get; set; }
    }
}
