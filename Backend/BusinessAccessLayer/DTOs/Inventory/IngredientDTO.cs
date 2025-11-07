using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.DTOs.Inventory
{
    public class IngredientDTO
    {
        public int IngredientId { get; set; }
        public string IngredientCode { get; set; } = string.Empty!;

        public string Name { get; set; } = null!;

        public string? Unit { get; set; }

        public decimal? ReorderLevel { get; set; }
    }
}
