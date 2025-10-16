using BusinessAccessLayer.DTOs.Manager;
using BusinessAccessLayer.Services;
using BusinessAccessLayer.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace SapaFoRestRMSAPI.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class ManagerCategoryController : ControllerBase
    {
        private readonly IManagerCategoryService _managerCategoryManager;

        public ManagerCategoryController(IManagerCategoryService managerCategoryManager)
        {
            _managerCategoryManager = managerCategoryManager;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ManagerCategoryDTO>>> GetManagerCategory()
        {
            try
            {
                // Get list category
                var category = await _managerCategoryManager.GetAllCategory();
                if (!category.Any())
                {
                    //Can't find category
                    return NotFound("No category found");
                }
                // Find list category
                return Ok(category);
            }
            //Error
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}
