using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.DTOs.ManagementCombo
{
    public class ComboDisplayDto
    {
        public int ComboId { get; set; }
        public string ComboName { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public decimal Price { get; set; }

        // Món trong combo
        public List<string> MenuItems { get; set; }

        // Lượt gọi
        public int WeeklyUsed { get; set; }
        public int MonthlyUsed { get; set; }
    }

}
