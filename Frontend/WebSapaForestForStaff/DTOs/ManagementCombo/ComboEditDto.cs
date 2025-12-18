using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebSapaForestForStaff.DTOs.ManagementCombo
{
    public class ComboEditDto
    {
        // ====== COMBO INFO ======
        public int ComboId { get; set; }

        public string Name { get; set; }
        public string Description { get; set; }

        public decimal SellingPrice { get; set; }
        public bool IsAvailable { get; set; }

        public string ImageUrl { get; set; }

        // ====== ITEMS ======
        public List<ComboItemUpdateDTO> Items { get; set; } = new();

        // ====== CALCULATED (FOR VIEW) ======
        public decimal TotalPrice
            => Items?.Sum(i => i.OriginalPrice * i.Quantity) ?? 0m;

        public decimal SavingsAmount
            => TotalPrice - SellingPrice;
    }
}
