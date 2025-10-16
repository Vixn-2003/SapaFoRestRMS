using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.DTOs
{
    public class AreaSuggestionDto
    {
        public int AreaId { get; set; }
        public string AreaName { get; set; } = null!;
        public List<TableDto> AllAvailableTables { get; set; } = new();
        public List<TableDto> SuggestedSingleTables { get; set; } = new();
        public List<List<TableDto>> SuggestedCombos { get; set; } = new();
    }
}
