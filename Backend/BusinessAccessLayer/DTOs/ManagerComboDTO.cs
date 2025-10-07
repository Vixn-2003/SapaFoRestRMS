using DomainAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.DTOs
{
    public class ManagerComboDTO
    {

        public int ComboId { get; set; }

        public string Name { get; set; } = null!;

        public string? Description { get; set; }

        public decimal Price { get; set; }

        public bool? IsAvailable { get; set; }
        public string? LinkImg { get; set; }

        public List<ManagerComboItemDTO?> ComboItems { get; set; } = new List<ManagerComboItemDTO?>();
    }
}
