using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.DTOs.Inventory
{
    public class InventoryBatchDTO
    {
        public int BatchId { get; set; }
        public decimal QuantityRemaining { get; set; }
        public DateOnly? ExpiryDate { get; set; }
        public DateTime? CreatedAt { get; set; }

        public List<StockTransactionDTO> StockTransactions { get; set; } = new();
    }
}
