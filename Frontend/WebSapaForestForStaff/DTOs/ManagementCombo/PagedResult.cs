using System.Text.Json.Serialization; // QUAN TRỌNG: Dùng thư viện này, không dùng Newtonsoft

namespace WebSapaForestForStaff.DTOs.ManagementCombo
{
    public class PagedResult<T>
    {
        // 1. Thêm Constructor rỗng để tránh lỗi "Deserialization constructor"
        public PagedResult() { }

        // 2. Nếu muốn giữ Constructor cũ để code chỗ khác dùng thì cứ để, nhưng phải có cái rỗng ở trên
        public PagedResult(List<T> items, int count, int pageIndex, int pageSize)
        {
            Items = items;
            TotalRecords = count;
            PageIndex = pageIndex;
            PageSize = pageSize;
        }

        [JsonPropertyName("items")]
        public List<T> Items { get; set; } = new List<T>();

        [JsonPropertyName("totalRecords")]
        public int TotalRecords { get; set; }

        [JsonPropertyName("pageIndex")]
        public int PageIndex { get; set; }

        [JsonPropertyName("pageSize")]
        public int PageSize { get; set; }

        // Thuộc tính tính toán chỉ đọc, không cần hứng từ JSON
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalRecords / PageSize) : 0;
    }
}