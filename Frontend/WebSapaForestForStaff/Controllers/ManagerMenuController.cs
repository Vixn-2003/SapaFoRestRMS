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
            try
            {
                var responseMenu = await _httpClient.GetAsync("api/ManagerMenu/statistics");

                if (responseMenu.IsSuccessStatusCode)
                {
                    var jsonMenu = await responseMenu.Content.ReadAsStringAsync();

                    // ✅ Debug: In ra JSON để kiểm tra
                    System.Diagnostics.Debug.WriteLine("JSON Response: " + jsonMenu);

                    var settings = new JsonSerializerSettings
                    {
                        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                        NullValueHandling = NullValueHandling.Ignore
                    };

                    // ✅ ĐÚNG: Deserialize vào ApiResponse wrapper
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<List<MenuItemDTO>>>(jsonMenu, settings);

                    // ✅ Lấy data từ wrapper
                    var productsMenu = apiResponse?.Data ?? new List<MenuItemDTO>();

                    // ✅ Debug: Kiểm tra số lượng items
                    System.Diagnostics.Debug.WriteLine($"Total items loaded: {productsMenu.Count}");

                    return View("~/Views/Menu/ManagerMenu.cshtml", productsMenu);
                }
                else
                {
                    var errorContent = await responseMenu.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"API Error: {responseMenu.StatusCode} - {errorContent}");
                    return View("~/Views/Menu/ManagerMenu.cshtml", new List<MenuItemDTO>());
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Exception: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                return View("~/Views/Menu/ManagerMenu.cshtml", new List<MenuItemDTO>());
            }
        }

        [HttpGet] // ✅ THÊM DÒNG NÀY
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

        [HttpGet]
        public async Task<ActionResult> Create()
        {
            try
            {
                // Chỉ lấy Category và Ingredient
                var responseCategory = await _httpClient.GetAsync("api/ManagerCategory");
                var responseIngredient = await _httpClient.GetAsync("api/InventoryIngredient");

                if (!responseCategory.IsSuccessStatusCode || !responseIngredient.IsSuccessStatusCode)
                {
                    return StatusCode(500, "Không thể tải dữ liệu từ server");
                }

                var jsonCategory = await responseCategory.Content.ReadAsStringAsync();
                var jsonIngredient = await responseIngredient.Content.ReadAsStringAsync();

                var category = JsonConvert.DeserializeObject<List<ManagerCategoryDTO>>(jsonCategory);
                var ingredient = JsonConvert.DeserializeObject<List<InventoryIngredientDTO>>(jsonIngredient);

                // ViewModel với menu mới (rỗng)
                var vm = new MenuViewModel
                {
                    ProductsMenu = new ManagerMenuDTO
                    {
                        IsAvailable = true, // ✅ Mặc định "Đang phục vụ"
                        BillingType = (ItemBillingType)2,    // ✅ Mặc định "Cần chế biến"
                        IsAds = false       // ✅ Mặc định không hiển thị trang chính
                    },
                    ProductsCategory = category ?? new(),
                    Ingredient = ingredient ?? new(),
                    Recipe = new() // Danh sách recipe rỗng
                };

                return View("~/Views/Menu/ManagerAddMenu.cshtml", vm);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi: {ex.Message}");
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateMenu()
        {
            try
            {
                // ✅ THÊM LOGGING
                System.Diagnostics.Debug.WriteLine("=== CreateMenu Called ===");
                System.Diagnostics.Debug.WriteLine($"Form Keys: {string.Join(", ", Request.Form.Keys)}");

                // Tạo MultipartFormDataContent
                using var formContent = new MultipartFormDataContent();

                // ✅ Lấy dữ liệu từ form - GIỐNG UpdateMenu
                var name = Request.Form["ProductsMenu.Name"].ToString();
                var categoryId = Convert.ToInt32(Request.Form["ProductsMenu.CategoryId"]);
                var price = Convert.ToDecimal(Request.Form["ProductsMenu.Price"]);
                var isAvailable = Convert.ToBoolean(Request.Form["ProductsMenu.IsAvailable"]);
                var courseType = Request.Form["ProductsMenu.CourseType"].ToString();
                var description = Request.Form["ProductsMenu.Description"].ToString();

                int? timeCook = null;
                if (Request.Form.ContainsKey("ProductsMenu.TimeCook") &&
                    !string.IsNullOrEmpty(Request.Form["ProductsMenu.TimeCook"]))
                {
                    timeCook = Convert.ToInt32(Request.Form["ProductsMenu.TimeCook"]);
                }

                int? batchSize = null;
                if (Request.Form.ContainsKey("ProductsMenu.BatchSize") &&
                    !string.IsNullOrEmpty(Request.Form["ProductsMenu.BatchSize"]))
                {
                    batchSize = Convert.ToInt32(Request.Form["ProductsMenu.BatchSize"]);
                }

                var billingType = Convert.ToInt32(Request.Form["ProductsMenu.BillingType"]);
                var isAds = Convert.ToBoolean(Request.Form["ProductsMenu.IsAds"]); // ✅ ĐỔI THÀNH ToBoolean

                // ✅ Validate
                if (string.IsNullOrEmpty(name))
                {
                    return BadRequest(new { message = "Tên món ăn không được để trống" });
                }

                // ✅ Thêm các trường vào form content - GIỐNG UpdateMenu
                formContent.Add(new StringContent(name), "Name");
                formContent.Add(new StringContent(categoryId.ToString()), "CategoryId");
                formContent.Add(new StringContent(price.ToString()), "Price");
                formContent.Add(new StringContent(isAvailable.ToString().ToLower()), "IsAvailable");
                formContent.Add(new StringContent(courseType), "CourseType");
                formContent.Add(new StringContent(description ?? ""), "Description");

                if (timeCook.HasValue)
                {
                    formContent.Add(new StringContent(timeCook.Value.ToString()), "TimeCook");
                }
                if (batchSize.HasValue)
                {
                    formContent.Add(new StringContent(batchSize.Value.ToString()), "BatchSize");
                }

                formContent.Add(new StringContent(billingType.ToString()), "BillingType");
                formContent.Add(new StringContent(isAds.ToString().ToLower()), "IsAds");

                // 🖼️ Xử lý file ảnh (NẾU CÓ) - GIỐNG UpdateMenu
                var imageFile = Request.Form.Files["ImageFile"];
                if (imageFile != null && imageFile.Length > 0)
                {
                    var fileContent = new StreamContent(imageFile.OpenReadStream());
                    fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(imageFile.ContentType);
                    formContent.Add(fileContent, "imageFile", imageFile.FileName);

                    System.Diagnostics.Debug.WriteLine($"Sending image file: {imageFile.FileName} ({imageFile.Length} bytes)");
                }

                // 🍽️ Lấy danh sách recipes - GIỐNG UpdateMenu
                var recipes = new List<object>();
                int index = 0;
                while (Request.Form.ContainsKey($"MenuRecipes[{index}].IngredientId"))
                {
                    var ingredientId = Convert.ToInt32(Request.Form[$"MenuRecipes[{index}].IngredientId"]);
                    var quantity = Convert.ToDecimal(Request.Form[$"MenuRecipes[{index}].QuantityNeeded"]);

                    recipes.Add(new
                    {
                        ingredientId = ingredientId,
                        quantity = quantity
                    });

                    index++;
                }

                // ✅ Validate recipes
                if (recipes.Count == 0)
                {
                    return BadRequest(new { message = "Vui lòng chọn ít nhất một nguyên liệu" });
                }

                // ✅ Thêm recipes dưới dạng JSON string - GIỐNG UpdateMenu
                var recipesJson = JsonConvert.SerializeObject(recipes);
                formContent.Add(new StringContent(recipesJson, Encoding.UTF8, "application/json"), "RecipesJson");

                System.Diagnostics.Debug.WriteLine($"Sending recipes JSON: {recipesJson}");

                // 📡 Gọi API - CHỈ KHÁC Ở ĐÂY: POST thay vì PUT, và endpoint "create" thay vì "update"
                var response = await _httpClient.PostAsync("api/ManagerMenu/create", formContent);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"API Response: {responseContent}");

                    return Ok(new { message = "Tạo món ăn mới thành công!" });
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"API Error: {response.StatusCode} - {errorContent}");

                    return StatusCode((int)response.StatusCode,
                        new { message = "Lỗi khi tạo món ăn", details = errorContent });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Exception: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");

                return StatusCode(500, new { message = "Có lỗi xảy ra", error = ex.Message });
            }
        }


        [HttpPost]
        public async Task<IActionResult> UpdateMenu()
        {
            try
            {
                // Tạo MultipartFormDataContent
                using var formContent = new MultipartFormDataContent();

                // Lấy dữ liệu từ form
                var menuItemId = Convert.ToInt32(Request.Form["ProductsMenu.MenuItemId"]);
                var name = Request.Form["ProductsMenu.Name"].ToString();
                var categoryId = Convert.ToInt32(Request.Form["ProductsMenu.CategoryId"]);
                var price = Convert.ToDecimal(Request.Form["ProductsMenu.Price"]);
                var isAvailable = Convert.ToBoolean(Request.Form["ProductsMenu.IsAvailable"]);
                var courseType = Request.Form["ProductsMenu.CourseType"].ToString();
                var description = Request.Form["ProductsMenu.Description"].ToString();
                var imageUrl = Request.Form["ProductsMenu.ImageUrl"].ToString(); // URL cũ

                int? timeCook = null;
                if (Request.Form.ContainsKey("ProductsMenu.TimeCook") &&
                    !string.IsNullOrEmpty(Request.Form["ProductsMenu.TimeCook"]))
                {
                    timeCook = Convert.ToInt32(Request.Form["ProductsMenu.TimeCook"]);
                }

                int? batchSize = null;
                if (Request.Form.ContainsKey("ProductsMenu.BatchSize") &&
                    !string.IsNullOrEmpty(Request.Form["ProductsMenu.BatchSize"]))
                {
                    batchSize = Convert.ToInt32(Request.Form["ProductsMenu.BatchSize"]);
                }

                var billingType = Convert.ToInt32(Request.Form["ProductsMenu.BillingType"]);
                var isAds = Convert.ToBoolean(Request.Form["ProductsMenu.IsAds"]);

                // ✅ Thêm các trường vào form content
                formContent.Add(new StringContent(menuItemId.ToString()), "MenuId");
                formContent.Add(new StringContent(name), "Name");
                formContent.Add(new StringContent(categoryId.ToString()), "CategoryId");
                formContent.Add(new StringContent(price.ToString()), "Price");
                formContent.Add(new StringContent(isAvailable.ToString().ToLower()), "IsAvailable");
                formContent.Add(new StringContent(courseType), "CourseType");
                formContent.Add(new StringContent(description ?? ""), "Description");
                formContent.Add(new StringContent(imageUrl ?? ""), "ImageUrl"); // Gửi URL cũ

                if (timeCook.HasValue)
                {
                    formContent.Add(new StringContent(timeCook.Value.ToString()), "TimeCook");
                }
                if (batchSize.HasValue)
                {
                    formContent.Add(new StringContent(batchSize.Value.ToString()), "BatchSize");
                }

                formContent.Add(new StringContent(billingType.ToString()), "BillingType");
                formContent.Add(new StringContent(isAds.ToString().ToLower()), "IsAds");

                // 🖼️ Xử lý file ảnh (NẾU CÓ)
                var imageFile = Request.Form.Files["ImageFile"];
                if (imageFile != null && imageFile.Length > 0)
                {
                    var fileContent = new StreamContent(imageFile.OpenReadStream());
                    fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(imageFile.ContentType);
                    formContent.Add(fileContent, "imageFile", imageFile.FileName);

                    System.Diagnostics.Debug.WriteLine($"Sending image file: {imageFile.FileName} ({imageFile.Length} bytes)");
                }

                // 🍽️ Lấy danh sách recipes
                var recipes = new List<object>();
                int index = 0;
                while (Request.Form.ContainsKey($"MenuRecipes[{index}].IngredientId"))
                {
                    var ingredientId = Convert.ToInt32(Request.Form[$"MenuRecipes[{index}].IngredientId"]);
                    var quantity = Convert.ToDecimal(Request.Form[$"MenuRecipes[{index}].QuantityNeeded"]);

                    recipes.Add(new
                    {
                        ingredientId = ingredientId,
                        quantity = quantity
                    });

                    index++;
                }

                // ✅ Validate
                if (string.IsNullOrEmpty(name))
                {
                    return BadRequest(new { message = "Tên món ăn không được để trống" });
                }

                if (recipes.Count == 0)
                {
                    return BadRequest(new { message = "Vui lòng chọn ít nhất một nguyên liệu" });
                }

                // ✅ Thêm recipes dưới dạng JSON string
                var recipesJson = JsonConvert.SerializeObject(recipes);
                formContent.Add(new StringContent(recipesJson, Encoding.UTF8, "application/json"), "RecipesJson");

                System.Diagnostics.Debug.WriteLine($"Sending recipes JSON: {recipesJson}");

                // 📡 Gọi API
                var response = await _httpClient.PutAsync("api/ManagerMenu/update", formContent);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"API Response: {responseContent}");

                    return Ok(new { message = "Cập nhật thành công" });
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"API Error: {response.StatusCode} - {errorContent}");

                    return StatusCode((int)response.StatusCode,
                        new { message = "API error", details = errorContent });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Exception: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");

                return StatusCode(500, new { message = "Có lỗi xảy ra", error = ex.Message });
            }
        }
    
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