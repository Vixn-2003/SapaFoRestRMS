namespace WebSapaForestForStaff.DTOs.Positions
{
    public class PositionDto
    {
        public int PositionId { get; set; }
        public string PositionName { get; set; } = null!;
        public string? Description { get; set; }
        public int Status { get; set; }
    }

    public class PositionCreateRequest
    {
        public string PositionName { get; set; } = null!;
        public string? Description { get; set; }
        public int Status { get; set; } = 0;
    }

    public class PositionUpdateRequest
    {
        public int PositionId { get; set; }
        public string PositionName { get; set; } = null!;
        public string? Description { get; set; }
        public int Status { get; set; }
    }

    public class PositionSearchRequest
    {
        public string? SearchTerm { get; set; }
        public int? Status { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class PositionListResponse
    {
        public List<PositionDto> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public bool HasPreviousPage { get; set; }
        public bool HasNextPage { get; set; }
    }
}


