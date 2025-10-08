using BusinessAccessLayer.DTOs;
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
        private readonly IManagerMenuService _managerMenuService ;

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
    
    }   
}
