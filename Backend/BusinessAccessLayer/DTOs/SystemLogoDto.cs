using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.DTOs
{
    public class SystemLogoDto
    {
        public int LogoId { get; set; }
        public string LogoName { get; set; } = null!;
        public string LogoUrl { get; set; } = null!;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
    }
}
