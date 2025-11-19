using DomainAccessLayer.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.DTOs.Inventory
{
    public class UnitDTO
    {
        public int UnitId { get; set; }

        public string UnitName { get; set; } = null!;

        public UnitType UnitType { get; set; }
    }
}
