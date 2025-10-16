using BusinessAccessLayer.DTOs;
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

            if (responseMenu.IsSuccessStatusCode && responseCombo.IsSuccessStatusCode)
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

            if (!response.IsSuccessStatusCode)
            {
                return NotFound("Không tìm thấy món ăn với ID được chọn.");
            }

            var jsonData = await response.Content.ReadAsStringAsync();
            var jsonCategory = await responseCategory.Content.ReadAsStringAsync();
            var jsonIngredient = await responseIngredient.Content.ReadAsStringAsync();
            var menu = JsonConvert.DeserializeObject<ManagerMenuDTO>(jsonData);
            var category = JsonConvert.DeserializeObject<List<ManagerCategoryDTO>>(jsonCategory);
            var ingredient = JsonConvert.DeserializeObject<List<InventoryIngredientWithBatchDTO>>(jsonIngredient);

            var vm = new MenuViewModel
            {
                ProductsMenu = menu ?? new(),               
                ProductsCategory = category ?? new(),
                Ingredient = ingredient ?? new()
            };

            return View("~/Views/Menu/ManagerEditMenu.cshtml", vm);
        }

        // POST: Menu/UpdateMenu
        [HttpPost]
        public async Task<IActionResult> UpdateMenu([FromForm] UpdateMenuRequest request)
        {
            try
            {
                // Validate dữ liệu
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { message = "Dữ liệu không hợp lệ", errors = ModelState.Values });
                }

                // Xử lý upload file ảnh (nếu có)
                string imageUrl = null;

                // Chuẩn bị dữ liệu để gửi API
                var menuData = new
                {
                    menuId = request.ProductsMenu.MenuItemId,
                    name = request.ProductsMenu.Name,
                    categoryId = request.ProductsMenu.CategoryId,
                    price = request.ProductsMenu.Price,
                    isAvailable = request.ProductsMenu.IsAvailable,
                    courseType = request.ProductsMenu.CourseType,
                    description = request.ProductsMenu.Description,
                    imageUrl = imageUrl ?? request.ProductsMenu.ImageUrl,
                    recipes = request.MenuRecipes.Select(i => new
                    {
                        ingredientId = i.IngredientId,
                        quantity = i.QuantityNeeded
                    }).ToList()
                };

                // Gọi API để cập nhật
                var jsonContent = JsonConvert.SerializeObject(menuData);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync($"api/menu/update", content);

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

        public List<InventoryIngredientWithBatchDTO> Ingredient { get; set; } = new();
        public List<ManagerCategoryDTO> ProductsCategory { get; set; } = new();
    }

    public class UpdateMenuRequest
    {
        public ManagerMenuDTO ProductsMenu { get; set; }
        public IFormFile ImageFile { get; set; }
        public List<RecipeDTO> MenuRecipes { get; set; }
    }
}
