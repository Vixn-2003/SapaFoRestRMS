using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace BusinessAccessLayer.DTOs.Manager
{
    // DTO cho API request nhận từ MultipartFormData
    public class FormUpdateMenuDTO
    {
        public int MenuId { get; set; }
        public string Name { get; set; }
        public int CategoryId { get; set; }
        public decimal Price { get; set; }
        public bool IsAvailable { get; set; }
        public string CourseType { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; } // URL cũ (nếu không đổi ảnh)

        //  3 TRƯỜNG MỚI
        public int? TimeCook { get; set; }
        public int BillingType { get; set; }
        public bool IsAds { get; set; }

        //  Recipes được gửi dưới dạng JSON string trong form
        public string RecipesJson { get; set; }

        // Property để parse recipes từ JSON
        private List<RecipeItemRequest> _recipes;

        [JsonIgnore] // Không serialize field này
        public List<RecipeItemRequest> Recipes
        {
            get
            {
                if (_recipes == null && !string.IsNullOrEmpty(RecipesJson))
                {
                    try
                    {
                        _recipes = JsonConvert.DeserializeObject<List<RecipeItemRequest>>(RecipesJson);
                    }
                    catch
                    {
                        _recipes = new List<RecipeItemRequest>();
                    }
                }
                return _recipes ?? new List<RecipeItemRequest>();
            }
            set => _recipes = value;
        }
    }

    public class RecipeItemRequest
    {
        public int IngredientId { get; set; }
        public decimal Quantity { get; set; }
    }
}