using BusinessAccessLayer.DTOs;
using BusinessAccessLayer.Services.Interfaces;
using DataAccessLayer.UnitOfWork.Interfaces;
using DomainAccessLayer.Models;
using Microsoft.AspNetCore.Mvc;

namespace SapaFoRestRMSAPI.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class MenuController : ControllerBase
    {
        private readonly IMenuService _menuService ;

        public MenuController(IMenuService menuService)
        {
            _menuService = menuService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<MenuDTO>>> GetMenu()
        {
            try
            {
                var Menu = await _menuService.GetAllMenu();
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
    
    }   
}
