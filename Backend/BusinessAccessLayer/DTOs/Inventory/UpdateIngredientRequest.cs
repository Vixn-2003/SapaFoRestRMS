using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.DTOs.Inventory
{
    public class UpdateIngredientRequest
    {
        public int IngredientId { get; set; }
        public string Name { get; set; }
        public string Unit { get; set; }
    }
}
