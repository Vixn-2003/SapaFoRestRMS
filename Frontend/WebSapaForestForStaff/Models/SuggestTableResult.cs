namespace WebSapaForestForStaff.Models
{
    public class SuggestTableResult
    {
        public List<AreaSuggest> Areas { get; set; } = new List<AreaSuggest>();
    }

    public class AreaSuggest
    {
        public int AreaId { get; set; }
        public string AreaName { get; set; } = null!;
        // Bàn đơn đủ sức chứa
        public List<TableDto> SuggestedSingleTables { get; set; } = new List<TableDto>();

        // Các combo bàn (các list bàn kết hợp đủ sức chứa)
        public List<List<TableDto>> SuggestedCombos { get; set; } = new List<List<TableDto>>();
    }

    public class TableDto
    {
        public int TableId { get; set; }
        public string TableName { get; set; } = null!;
        public int Capacity { get; set; }
    }
}
