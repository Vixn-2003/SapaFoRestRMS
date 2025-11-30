using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.DTOs.Inventory
{
    public class StockTransactionDTO
    {
        public int TransactionId { get; set; }

        public int IngredientId { get; set; }
        public string? IngredientName { get; set; }

        public string Type { get; set; } = null!;

        public decimal Quantity { get; set; }

        public DateTime? TransactionDate { get; set; }

        public string? Note { get; set; }

        public int? BatchId { get; set; }
        public string? BatchName { get; set; }
    }
}
