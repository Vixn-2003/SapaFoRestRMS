using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.DTOs.RestaurantIntroDto
{
    public class RestaurantIntroDto
    {
        public int IntroId { get; set; }
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public string? VideoUrl { get; set; }
        public bool? IsActive { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }
}
