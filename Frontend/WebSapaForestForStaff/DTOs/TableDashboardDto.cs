using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace WebSapaForestForStaff.DTOs
{
    public class TableDashboardDto
    {
        [JsonPropertyName("tableId")]
        public int TableId { get; set; }

        [JsonPropertyName("tableNumber")]
        public string TableNumber { get; set; } // Đây là "Tên bàn" (ví dụ: "A1-01")

        [JsonPropertyName("areaName")]
        public string AreaName { get; set; }

        [JsonPropertyName("floor")]
        public int Floor { get; set; }

        [JsonPropertyName("capacity")]
        public int Capacity { get; set; } // Sức chứa

        [JsonPropertyName("status")]
        public string Status { get; set; } 

        [JsonPropertyName("guestCount")]
        public int GuestCount { get; set; } // Số khách đang ngồi

        [JsonPropertyName("guestSeatedTime")]
        public DateTime? GuestSeatedTime { get; set; } // Cần '?' vì nó có thể là null
    }
}
