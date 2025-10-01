namespace WebSapaFoRestForCustomer.Models
{
    public class EventDto
    {
        public string Title { get; set; } = null!;
        public string? ImageUrl { get; set; }
        public string? Description { get; set; }
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public string? Location { get; set; }
    }
}
