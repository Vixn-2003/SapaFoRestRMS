using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.DTOs.Inventory
{
    public class SupplierComparisonDTO
    {
        public int SupplierId { get; set; }
        public string SupplierName { get; set; } = null!;
        public string SupplierCode { get; set; } = null!;
        public string Unit { get; set; } = null!;
        public decimal RecentPrice { get; set; }
        public decimal TotalQuantity { get; set; }
        public int TransactionCount { get; set; }
        public DateTime? LastOrderDate { get; set; }
    }
}
