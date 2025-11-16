using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainAccessLayer.Models
{
    public partial class Warehouse
    {
        public int WarehouseId { get; set; }     

        public string Name { get; set; } = null!;  

        public bool IsActive { get; set; } = true; 

        public virtual ICollection<InventoryBatch> InventoryBatches { get; set; } = new List<InventoryBatch>();
        public virtual ICollection<PurchaseOrderDetail> PurchaseOrderDetails { get; set; } = new List<PurchaseOrderDetail>();
    }
}
