using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace BusinessAccessLayer.DTOs
{
    public class BrandBannerUpdateDto
    {
        public int BannerId { get; set; } // dùng cho update
        public string Title { get; set; }
        public IFormFile ImageFile { get; set; } // file upload
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public string? Status { get; set; } // active/inactive
    }
}
