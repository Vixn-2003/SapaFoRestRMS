using BusinessAccessLayer.DTOs.Manager;
using BusinessAccessLayer.Services.Interfaces;
using DataAccessLayer.UnitOfWork.Interfaces;
using DomainAccessLayer.Models;
using Microsoft.AspNetCore.Mvc;

namespace SapaFoRestRMSAPI.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class ManagerMenuController : ControllerBase
    {
        private readonly IManagerMenuService _managerMenuService;

        public ManagerMenuController(IManagerMenuService managerMenuService)
        {
            _managerMenuService = managerMenuService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ManagerMenuDTO>>> GetManagerMenu()
        {
            try
            {
                var Menu = await _managerMenuService.GetManagerAllMenu();
                if (!Menu.Any())
                {
                    return NotFound("No menu found");
                }
                return Ok(Menu);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while getting the menu");
            }
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


        [HttpPut("update")]
        public async Task<IActionResult> UpdateMenu([FromBody] FormUpdateMenuDTO request)
        {
            try
            {
                if (request == null || request.MenuId <= 0)
                    return BadRequest("Invalid menu data");

                // 🧩 1. Cập nhật thông tin món
                var managerMenuDTO = new ManagerMenuDTO
                {
                    MenuItemId = request.MenuId,
                    Name = request.Name,
                    CategoryId = request.CategoryId,
                    Price = request.Price,
                    IsAvailable = request.IsAvailable,
                    CourseType = request.CourseType,
                    Description = request.Description,
                    ImageUrl = request.ImageUrl
                };

                var resultMenu = await _managerMenuService.UpdateManagerMenu(managerMenuDTO);

                // 🧩 2. Cập nhật danh sách nguyên liệu
                if (request.Recipes != null && request.Recipes.Any())
                {
                    // Xóa toàn bộ công thức cũ của món này
                    await _managerMenuService.DeleteRecipeByMenuItemId(request.MenuId);

                    // Thêm lại danh sách mới
                    foreach (var recipe in request.Recipes)
                    {
                        var recipeDTO = new RecipeDTO
                        {
                            MenuItemId = request.MenuId,
                            IngredientId = recipe.IngredientId,
                            QuantityNeeded = recipe.Quantity
                        };
                        await _managerMenuService.AddRecipe(recipeDTO);
                    }
                }

                return Ok(new { message = "Menu updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

    }
}
