using BusinessAccessLayer.DTOs.Manager;
using BusinessAccessLayer.Services;
using BusinessAccessLayer.Services.Interfaces;
using DataAccessLayer.UnitOfWork.Interfaces;
using DomainAccessLayer.Enums;
using DomainAccessLayer.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace SapaFoRestRMSAPI.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class ManagerMenuController : ControllerBase
    {
        private readonly IManagerMenuService _managerMenuService;
        private readonly ICloudinaryService _cloudinaryService;

        public ManagerMenuController(IManagerMenuService managerMenuService, ICloudinaryService cloudinaryService)
        {
            _managerMenuService = managerMenuService;
            _cloudinaryService = cloudinaryService;
        }

        [HttpGet("statistics")]
        public async Task<IActionResult> GetStatistics()
        {
            var data = await _managerMenuService.GetMenuItemsWithStatisticsAsync();

            return Ok(new
            {
                success = true,
                data = data,
                total = data.Count
            });
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ManagerMenuDTO>> ManagerMenuById(int id)
        {
            try
            {
                var menu = await _managerMenuService.ManagerMenuById(id);

                if (menu == null)
                {
                    return NotFound("No menu found");
                }

                return Ok(menu);
            }
            catch (Exception ex)
            {
                // Ghi log lỗi nếu cần
                return StatusCode(500, $"An error occurred while getting the menu: {ex.Message}");
            }
        }

        [HttpGet("recipes/{menuId}")]
        public async Task<ActionResult<RecipeDTO>> ListRecipeMenuItem(int menuId)
        {
            try
            {
                var recipe = await _managerMenuService.GetRecipeByMenuItem(menuId);

                if (recipe == null)
                {
                    return NotFound("No recipe found");
                }

                return Ok(recipe);
            }
            catch (Exception ex)
            {
                // Ghi log lỗi nếu cần
                return StatusCode(500, $"An error occurred while getting the menu: {ex.Message}");
            }
        }


        [HttpPost("create")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreateMenu(
    [FromForm] string Name,
    [FromForm] int CategoryId,
    [FromForm] decimal Price,
    [FromForm] bool IsAvailable,
    [FromForm] string CourseType,
    [FromForm] string Description = "",
    [FromForm] int? TimeCook = null,
    [FromForm] int? BatchSize = null,
    [FromForm] int? BillingType = 2,
    [FromForm] bool IsAds = false,
    [FromForm] string RecipesJson = "",
    IFormFile imageFile = null)
        {
            try
            {
                //  BƯỚC 1: Validate dữ liệu
                if (string.IsNullOrWhiteSpace(Name))
                    return BadRequest(new { success = false, message = "Tên món ăn không được để trống" });

                if (!Enum.IsDefined(typeof(ItemBillingType), BillingType))
                    return BadRequest(new { success = false, message = "BillingType không hợp lệ" });

                //  BƯỚC 2: Kiểm tra trùng tên món ăn
                var existingMenu = await _managerMenuService.GetMenuByName(Name.Trim());
                if (existingMenu != null)
                {
                    return BadRequest(new { success = false, message = $"Món ăn '{Name}' đã tồn tại trong hệ thống!" });
                }

                //  BƯỚC 3: Upload ảnh nếu có
                string imageUrl = "";
                if (imageFile != null && imageFile.Length > 0)
                {
                    Console.WriteLine($"Uploading: {imageFile.FileName}");
                    imageUrl = await _cloudinaryService.UploadImageAsync(imageFile, "menu_items");
                }

                //  BƯỚC 4: Tạo DTO cho menu mới
                var managerMenuDTO = new ManagerMenuDTO
                {
                    // KHÔNG CẦN MenuItemId - Database sẽ tự động tạo
                    Name = Name.Trim(),
                    CategoryId = CategoryId,
                    Price = Price,
                    IsAvailable = IsAvailable,
                    CourseType = CourseType,
                    Description = Description?.Trim(),
                    ImageUrl = imageUrl,
                    TimeCook = TimeCook,
                    BatchSize = BatchSize,
                    BillingType = (ItemBillingType)BillingType,
                    IsAds = IsAds
                };

                //  BƯỚC 5: Tạo menu mới trong database
                var createdMenuId = await _managerMenuService.CreateManagerMenu(managerMenuDTO);

                if (createdMenuId <= 0)
                    return StatusCode(500, new { success = false, message = "Không thể tạo món ăn mới" });

                Console.WriteLine($" Created menu with ID: {createdMenuId}");

                //  BƯỚC 6: Xử lý recipes
                if (!string.IsNullOrWhiteSpace(RecipesJson))
                {
                    var recipesList = JsonConvert.DeserializeObject<List<RecipeItemRequest>>(RecipesJson);

                    if (recipesList != null && recipesList.Any())
                    {
                        foreach (var recipe in recipesList)
                        {
                            await _managerMenuService.AddRecipe(new RecipeDTO
                            {
                                MenuItemId = createdMenuId, //  Dùng ID vừa tạo
                                IngredientId = recipe.IngredientId,
                                QuantityNeeded = recipe.Quantity
                            });
                        }

                        Console.WriteLine($" Added {recipesList.Count} recipes to menu {createdMenuId}");
                    }
                }

                return Ok(new
                {
                    success = true,
                    message = "Tạo món ăn mới thành công!",
                    data = new
                    {
                        MenuId = createdMenuId,
                        Name,
                        ImageUrl = imageUrl
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error creating menu: {ex.Message}");
                Console.WriteLine($"❌ Stack trace: {ex.StackTrace}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }


        [HttpPut("update")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdateMenu(
    [FromForm] int MenuId,
    [FromForm] string Name,
    [FromForm] int CategoryId,
    [FromForm] decimal Price,
    [FromForm] bool IsAvailable,
    [FromForm] string CourseType,
    [FromForm] string Description = "",
    [FromForm] string ImageUrl = "",
    [FromForm] int? TimeCook = null,
    [FromForm] int? BatchSize = null,
    [FromForm] int? BillingType = 2,
    [FromForm] bool IsAds = false,
    [FromForm] string RecipesJson = "",
    IFormFile imageFile = null)
        {
            try
            {
                if (MenuId <= 0)
                    return BadRequest(new { success = false, message = "Invalid menu ID" });

                if (string.IsNullOrWhiteSpace(Name))
                    return BadRequest(new { success = false, message = "Tên món ăn không được để trống" });

                if (!Enum.IsDefined(typeof(ItemBillingType), BillingType))
                    return BadRequest(new { success = false, message = "BillingType không hợp lệ" });

                // Upload ảnh nếu có
                string finalImageUrl = ImageUrl;
                if (imageFile != null && imageFile.Length > 0)
                {
                    Console.WriteLine($"Uploading: {imageFile.FileName}");
                    finalImageUrl = await _cloudinaryService.UploadImageAsync(imageFile, "menu_items");
                }

                // Cập nhật menu
                var managerMenuDTO = new ManagerMenuDTO
                {
                    MenuItemId = MenuId,
                    Name = Name.Trim(),
                    CategoryId = CategoryId,
                    Price = Price,
                    IsAvailable = IsAvailable,
                    CourseType = CourseType,
                    Description = Description?.Trim(),
                    ImageUrl = finalImageUrl,
                    TimeCook = TimeCook,
                    BatchSize = BatchSize,
                    BillingType = (ItemBillingType)BillingType,
                    IsAds = IsAds
                };

                var resultMenu = await _managerMenuService.UpdateManagerMenu(managerMenuDTO);

                if (!resultMenu)
                    return StatusCode(500, new { success = false, message = "Không thể cập nhật món ăn" });

                // Xử lý recipes
                if (!string.IsNullOrWhiteSpace(RecipesJson))
                {
                    var recipesList = JsonConvert.DeserializeObject<List<RecipeItemRequest>>(RecipesJson);

                    if (recipesList != null && recipesList.Any())
                    {
                        await _managerMenuService.DeleteRecipeByMenuItemId(MenuId);

                        foreach (var recipe in recipesList)
                        {
                            await _managerMenuService.AddRecipe(new RecipeDTO
                            {
                                MenuItemId = MenuId,
                                IngredientId = recipe.IngredientId,
                                QuantityNeeded = recipe.Quantity
                            });
                        }
                    }
                }

                return Ok(new
                {
                    success = true,
                    message = "Cập nhật món ăn thành công",
                    data = new { MenuId, Name, ImageUrl = finalImageUrl }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
    }
}
