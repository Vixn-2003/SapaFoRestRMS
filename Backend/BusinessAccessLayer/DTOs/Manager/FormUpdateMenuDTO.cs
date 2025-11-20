using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.DTOs.Manager
{
    // DTO cho API request
    public class FormUpdateMenuDTO
    {
        public int MenuId { get; set; }
        public string Name { get; set; }
        public int CategoryId { get; set; }
        public decimal Price { get; set; }
        public bool IsAvailable { get; set; }
        public string CourseType { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public List<RecipeItemRequest> Recipes { get; set; }
    }

    public class RecipeItemRequest
    {
        public int IngredientId { get; set; }
        public decimal Quantity { get; set; }
    }

}
