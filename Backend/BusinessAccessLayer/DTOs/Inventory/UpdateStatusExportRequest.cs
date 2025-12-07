using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.DTOs.Inventory
{
    public class UpdateStatusExportRequest
    {
        public int TransactionId { get; set; }
        public string StatusExport { get; set; } = null!;
    }
}
