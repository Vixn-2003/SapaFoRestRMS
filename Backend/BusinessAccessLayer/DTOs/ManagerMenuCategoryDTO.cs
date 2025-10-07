using DomainAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.DTOs
{
    public class ManagerMenuCategoryDTO
    {
        public int CategoryId { get; set; }

        public string CategoryName { get; set; } = null!;
    }
}
