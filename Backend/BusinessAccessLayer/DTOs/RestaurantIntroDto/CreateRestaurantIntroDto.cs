using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.DTOs.RestaurantIntroDto
{
    public class CreateRestaurantIntroDto
    {
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public IFormFile? Image { get; set; }
        public string? VideoUrl { get; set; }
        public bool IsActive { get; set; } = true;
        public int CreatedBy { get; set; }
    }
}
