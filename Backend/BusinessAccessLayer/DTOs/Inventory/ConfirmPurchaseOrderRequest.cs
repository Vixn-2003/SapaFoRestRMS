using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.DTOs.Inventory
{
    public class ConfirmPurchaseOrderRequest
    {
        public string PurchaseOrderId { get; set; } = null!;
        public int CheckId { get; set; }
        public DateTime TimeConfirm { get; set; }
        public string Status { get; set; } = null!; 
        public string? RejectReason { get; set; }   
    }

}
