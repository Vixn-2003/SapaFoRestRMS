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
        public string Type { get; set; } = null!;
        public decimal Quantity { get; set; }
        public DateTime? TransactionDate { get; set; }
        public string? Note { get; set; }
    }
}
