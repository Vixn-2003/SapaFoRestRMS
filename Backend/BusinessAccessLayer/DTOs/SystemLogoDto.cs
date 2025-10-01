using Microsoft.AspNetCore.Http;
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
        public IFormFile? File { get; set; }  // file upload
        public string? LogoUrl { get; set; }   // URL lưu trên Cloudinary
        public string? Description { get; set; }
        public bool? IsActive { get; set; } = true;
    }
}
