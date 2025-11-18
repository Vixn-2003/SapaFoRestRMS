namespace BusinessAccessLayer.DTOs.Positions
{
    public class PositionSearchRequest
    {
        public string? SearchTerm { get; set; }
        public int? Status { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}

