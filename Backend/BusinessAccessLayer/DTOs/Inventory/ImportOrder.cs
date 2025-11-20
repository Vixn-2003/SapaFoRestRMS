using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.DTOs.Inventory
{
    public class ImportOrder
    {
        public int Id { get; set; }
        public string ImportCode { get; set; } = null!;
        public DateTime ImportDate { get; set; }
        public int SupplierId { get; set; }
        public int CreatorId { get; set; }
        //public int? CheckId { get; set; }
        public string? ProofImagePath { get; set; }
        public string Status { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public decimal TotalAmount { get; set; }
    }
}
