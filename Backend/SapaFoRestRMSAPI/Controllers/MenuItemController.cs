using BusinessAccessLayer.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace SapaFoRestRMSAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MenuItemController : ControllerBase
    {
        private readonly IMenuItemService _menuItemService;

        public MenuItemController(IMenuItemService menuItemService)
        {
            _menuItemService = menuItemService;
        }

        [HttpGet("top-best-sellers")]
        public async Task<IActionResult> GetTopBestSellers()
        {
            var topItems = await _menuItemService.GetTopBestSellersAsync(10);
            return Ok(topItems);
        }
    }
}
