using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace WebSapaForestForStaff.DTOs
{
    public class DashboardDataDto
    {
        [JsonPropertyName("tables")]
        public List<TableDashboardDto> Tables { get; set; } = new();

        [JsonPropertyName("totalCount")]
        public int TotalCount { get; set; }

        [JsonPropertyName("areaNames")]
        public List<string> AreaNames { get; set; } = new();

        [JsonPropertyName("floors")]
        public List<int?> Floors { get; set; } = new();
    }
}
