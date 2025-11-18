using DomainAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.DTOs.Manager
{
    public class RecipeDTO
    {
        public int RecipeId { get; set; }

        public int MenuItemId { get; set; }

        public int IngredientId { get; set; }

        public decimal QuantityNeeded { get; set; }

    }
}
