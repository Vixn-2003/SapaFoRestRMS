using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.DTOs.Inventory
{
    public class IngredientUsageForecastDto
    {
        public int IngredientId { get; set; }
        public string IngredientName { get; set; } = null!;
        public string UnitName { get; set; } = null!;

        public decimal AverageDailyUsage { get; set; }
        public decimal SafetyDays { get; set; }

        public decimal SafetyStockQuantity { get; set; }
        public decimal ReorderLevel { get; set; }

        public decimal CurrentStock { get; set; }
        public decimal? DaysRemaining { get; set; }

        public int DaysWindowUsed { get; set; }
        public int DistinctUsedDays { get; set; }
        public decimal? CoefficientOfVariation { get; set; }
    }
}
