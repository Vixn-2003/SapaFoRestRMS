using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.DTOs.Inventory
{
    public class WarehouseDTO
    {
        public int WarehouseId { get; set; }

        public string Name { get; set; } = null!;

        public bool IsActive { get; set; } = true;
    }
}
