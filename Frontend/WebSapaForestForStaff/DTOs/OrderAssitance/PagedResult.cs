using System.Text.Json.Serialization;

namespace WebSapaForestForStaff.DTOs.OrderAssitance
{
    public class PagedResult<T>
    {
        [JsonPropertyName("items")]
        public IEnumerable<T> Items { get; set; } = new List<T>();

        [JsonPropertyName("totalCount")]
        public int TotalCount { get; set; }

        [JsonPropertyName("page")]
        public int Page { get; set; }

        [JsonPropertyName("pageSize")]
        public int PageSize { get; set; }

        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
    }
}
