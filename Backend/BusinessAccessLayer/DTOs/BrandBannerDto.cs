using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace BusinessAccessLayer.DTOs
{
    public class BrandBannerDto
    {
        public int BannerId { get; set; }
        public string? Title { get; set; }
        public string? ImageUrl { get; set; } 
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public string? Status { get; set; }
        public int? CreatedBy { get; set; }
        public string? CreatedByName { get; set; } 
    }
}
