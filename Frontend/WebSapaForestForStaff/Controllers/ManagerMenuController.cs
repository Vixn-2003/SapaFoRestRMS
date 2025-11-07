using BusinessAccessLayer.DTOs;
using BusinessAccessLayer.DTOs.Inventory;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;
using WebSapaForestForStaff.DTOs;
using WebSapaForestForStaff.Models;

namespace WebSapaForestForStaff.Controllers
{
    public class ManagerMenuController : Controller
    {
        private readonly HttpClient _httpClient;

        public ManagerMenuController(HttpClient httpClient)
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://localhost:7096/")
            };
        }

        // Hiển thị danh sách Menu + Combo
        public async Task<ActionResult> DisplayMenu()
        {
            var responseMenu = await _httpClient.GetAsync("api/ManagerMenu");
            var responseCombo = await _httpClient.GetAsync("api/ManagerCombo");
            var responseCategory = await _httpClient.GetAsync("api/ManagerCategory");

            if (responseMenu.IsSuccessStatusCode)
            {
                var jsonMenu = await responseMenu.Content.ReadAsStringAsync();
                var jsonCombo = await responseCombo.Content.ReadAsStringAsync();
                var jsonCategory = await responseCategory.Content.ReadAsStringAsync();
                var productsMenu = JsonConvert.DeserializeObject<List<ManagerMenuDTO>>(jsonMenu);
                var productsCombo = JsonConvert.DeserializeObject<List<ManagerComboDTO>>(jsonCombo);
                var productsCategory = JsonConvert.DeserializeObject<List<ManagerCategoryDTO>>(jsonCategory);

                var vm = new MenuComboViewModel
                {
                    ProductsMenu = productsMenu ?? new(),
                    ProductsCombo = productsCombo ?? new(),
                    ProductsCategory = productsCategory ?? new()
                };

                return View("~/Views/Menu/DashboardManager.cshtml", vm);
            }

            return View("~/Views/Menu/DashboardManager.cshtml", new MenuComboViewModel());
        }

        // ✅ POST: Nhận id từ form và hiển thị chi tiết món
        [HttpPost]
        public async Task<ActionResult> ManagerEditMenu(int id)
        {
            var response = await _httpClient.GetAsync($"/api/ManagerMenu/{id}");
            var responseCategory = await _httpClient.GetAsync("api/ManagerCategory");
            var responseIngredient = await _httpClient.GetAsync("api/InventoryIngredient");
            var responseRecipe = await _httpClient.GetAsync($"/api/ManagerMenu/recipes/{id}");

            if (!response.IsSuccessStatusCode)
            {
                return NotFound("Không tìm thấy món ăn với ID được chọn.");
            }

            var jsonData = await response.Content.ReadAsStringAsync();
            var jsonCategory = await responseCategory.Content.ReadAsStringAsync();
            var jsonIngredient = await responseIngredient.Content.ReadAsStringAsync();
            var jsonRecipe = await responseRecipe.Content.ReadAsStringAsync();


            var menu = JsonConvert.DeserializeObject<ManagerMenuDTO>(jsonData);
            var category = JsonConvert.DeserializeObject<List<ManagerCategoryDTO>>(jsonCategory);
            var ingredient = JsonConvert.DeserializeObject<List<InventoryIngredientDTO>>(jsonIngredient);
            var recipe = JsonConvert.DeserializeObject<List<ManagerRecipeDTO>>(jsonRecipe);

            var vm = new MenuViewModel
            {
                ProductsMenu = menu ?? new(),
                ProductsCategory = category ?? new(),
                Ingredient = ingredient ?? new(),
                Recipe = recipe ?? new()
            };

            return View("~/Views/Menu/ManagerEditMenu.cshtml", vm);
        }

        // POST: Menu/UpdateMenu
        [HttpPost]
        public async Task<IActionResult> UpdateMenu()
        {
            try
            {
                // Lấy dữ liệu từ form
                var menuItemId = Convert.ToInt32(Request.Form["ProductsMenu.MenuItemId"]);
                var name = Request.Form["ProductsMenu.Name"].ToString();
                var categoryId = Convert.ToInt32(Request.Form["ProductsMenu.CategoryId"]);
                var price = Convert.ToDecimal(Request.Form["ProductsMenu.Price"]);
                var isAvailable = Convert.ToBoolean(Request.Form["ProductsMenu.IsAvailable"]);
                var courseType = Request.Form["ProductsMenu.CourseType"].ToString();
                var description = Request.Form["ProductsMenu.Description"].ToString();
                var imageUrl = Request.Form["ProductsMenu.ImageUrl"].ToString();

                // Xử lý upload file ảnh (nếu có)
                var imageFile = Request.Form.Files["ImageFile"];
                if (imageFile != null && imageFile.Length > 0)
                {
                    // TODO: Xử lý upload file lên server/cloud
                    // imageUrl = await UploadImage(imageFile);
                }

                // Lấy danh sách recipes
                var recipe = new List<object>();
                int index = 0;
                while (Request.Form.ContainsKey($"MenuRecipes[{index}].IngredientId"))
                {
                    var ingredientId = Convert.ToInt32(Request.Form[$"MenuRecipes[{index}].IngredientId"]);
                    var quantity = Convert.ToDecimal(Request.Form[$"MenuRecipes[{index}].QuantityNeeded"]);

                    recipe.Add(new
                    {
                        ingredientId = ingredientId,
                        quantity = quantity
                    });

                    index++;
                }

                // Validate
                if (string.IsNullOrEmpty(name))
                {
                    return BadRequest(new { message = "Tên món ăn không được để trống" });
                }

                if (recipe.Count == 0)
                {
                    return BadRequest(new { message = "Vui lòng chọn ít nhất một nguyên liệu" });
                }

                // Chuẩn bị dữ liệu để gửi API
                var menuData = new
                {
                    menuId = menuItemId,
                    name = name,
                    categoryId = categoryId,
                    price = price,
                    isAvailable = isAvailable,
                    courseType = courseType,
                    description = description,
                    imageUrl = imageUrl,
                    recipes = recipe
                };

                // Gọi API để cập nhật
                var jsonContent = JsonConvert.SerializeObject(menuData);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync($"api/ManagerMenu/update", content);

                if (response.IsSuccessStatusCode)
                {
                    return Ok(new { message = "Cập nhật thành công" });
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return StatusCode((int)response.StatusCode, new { message = "API error", details = errorContent });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Có lỗi xảy ra", error = ex.Message });
            }
        }
    }

    public class MenuComboViewModel
    {
        public List<ManagerMenuDTO> ProductsMenu { get; set; } = new();
        public List<ManagerComboDTO> ProductsCombo { get; set; } = new();
        public List<ManagerCategoryDTO> ProductsCategory { get; set; } = new();
    }

    public class MenuViewModel
    {
        public ManagerMenuDTO ProductsMenu { get; set; } = new();

        public List<InventoryIngredientDTO> Ingredient { get; set; } = new();
        public List<ManagerCategoryDTO> ProductsCategory { get; set; } = new();
        public List<ManagerRecipeDTO> Recipe { get; set; } = new();
    }

    public class UpdateMenuRequest
    {
        public ManagerMenuDTO ProductsMenu { get; set; }
        public IFormFile ImageFile { get; set; }
        public List<RecipeDTO> MenuRecipes { get; set; }
    }
}