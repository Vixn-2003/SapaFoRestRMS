using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.DTOs.Inventory
{
    public class WarehouseBatchSummaryDTO
    {
        public int WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public int TotalBatches { get; set; }
        public List<InventoryBatchDTO> Batches { get; set; } = new List<InventoryBatchDTO>();
    }
}
