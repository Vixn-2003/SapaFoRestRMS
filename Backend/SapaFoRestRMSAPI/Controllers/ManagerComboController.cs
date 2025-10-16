using BusinessAccessLayer.DTOs.Manager;
using BusinessAccessLayer.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace SapaFoRestRMSAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ManagerComboController : ControllerBase
    {
        private readonly IManagerComboService _managerComboService;

        public ManagerComboController(IManagerComboService comboService)
        {
            _managerComboService = comboService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ManagerComboDTO>>> GetManagerCombo()
        {
            try
            {
                var combo = await _managerComboService.GetManagerAllCombo();
                if (!combo.Any())
                {
                    return NotFound("No menu found");
                }
                return Ok(combo);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

    }
}
