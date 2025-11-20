using DomainAccessLayer.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainAccessLayer.Models
{
    public class Unit
    {
        public int UnitId { get; set; }

        public string UnitName { get; set; } = null!;

        public UnitType UnitType { get; set; }

        public virtual ICollection<Ingredient> Ingredients { get; set; } = new List<Ingredient>();
    }
}
