using WebSapaForestForStaff.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebSapaForestForStaff.DTOs
{
    public class MenuCategoryDTO
    {
        public int CategoryId { get; set; }

        public string CategoryName { get; set; } = null!;
    }
}
