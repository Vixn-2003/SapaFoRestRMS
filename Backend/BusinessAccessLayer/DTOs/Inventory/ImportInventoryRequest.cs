using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.DTOs.Inventory
{
    public class ImportInventoryRequest
    {
        public string ImportCode { get; set; } = null!;
        public DateTime ImportDate { get; set; }
        public int SupplierId { get; set; }
        public int CreatorId { get; set; }
       // public int? CheckId { get; set; }
        public string Items { get; set; } = null!;
        public IFormFile? ProofFile { get; set; }
    }
}
