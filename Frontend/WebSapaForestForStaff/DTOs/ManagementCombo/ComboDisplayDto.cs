using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebSapaForestForStaff.DTOs.ManagementCombo
{
    public class ComboDisplayDto
    {
        [JsonProperty("comboId")]
        public int ComboId { get; set; }

        [JsonProperty("comboName")]
        public string ComboName { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("imageUrl")]
        public string ImageUrl { get; set; }

        [JsonProperty("price")]
        public decimal Price { get; set; }

        [JsonProperty("menuItems")]
        public List<string> MenuItems { get; set; } = new List<string>();

        [JsonProperty("weeklyUsed")]
        public int WeeklyUsed { get; set; }

        [JsonProperty("monthlyUsed")]
        public int MonthlyUsed { get; set; }
    }

}
