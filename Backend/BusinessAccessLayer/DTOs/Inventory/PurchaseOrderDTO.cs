using DomainAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.DTOs.Inventory
{
    public class PurchaseOrderDTO
    {
        public int PurchaseOrderId { get; set; }

        public int SupplierId { get; set; }

        public DateTime? OrderDate { get; set; }

        public string? Status { get; set; }

        public List<PurchaseOrderDetailDTO> PurchaseOrderDetails { get; set; }
    }
}
