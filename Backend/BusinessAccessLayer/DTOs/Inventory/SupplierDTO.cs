using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.DTOs.Inventory
{
    public class SupplierDTO
    {
        public int SupplierId { get; set; }

        public string Name { get; set; } = null!;

        public string? ContactInfo { get; set; }

        public string? Address { get; set; }

        public string? Phone { get; set; }

        public string? Email { get; set; }
        public string CodeSupplier { get; set; } = string.Empty!;

        [JsonIgnore]
        public List<PurchaseOrderDTO> PurchaseOrders { get; set; }
    }
}
