using WebSapaForestForStaff.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebSapaForestForStaff.DTOs
{
    public class ManagerComboItemDTO
    {
        public int ComboItemId { get; set; }

        public int ComboId { get; set; }

        public int MenuItemId { get; set; }

        public int Quantity { get; set; }

        public ManagerMenuDTO? ManagerMenuItem { get; set; }

    }
}
