using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.DTOs.Inventory
{
    public class ImportSubmitModel
    {
        public string SupplierName { get; set; } = string.Empty;
        public string CreatorName { get; set; } = string.Empty;
        public string CreatorPhone { get; set; } = string.Empty;
        public string CheckerName { get; set; } = string.Empty;
        public string CheckerPhone { get; set; } = string.Empty;
        public List<ImportMaterialItem> ImportList { get; set; } = new();
        public IFormFile? ProofFile { get; set; }
    }

    public class ImportMaterialItem
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}
