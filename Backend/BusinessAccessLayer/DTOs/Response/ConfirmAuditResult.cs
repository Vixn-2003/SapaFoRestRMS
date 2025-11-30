using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.DTOs.Response
{
    public class ConfirmAuditResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public string? AuditIdProcessing { get; set; }  
    }
}
