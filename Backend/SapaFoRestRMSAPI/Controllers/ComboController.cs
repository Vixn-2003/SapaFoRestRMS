using BusinessAccessLayer.DTOs;
using BusinessAccessLayer.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace SapaFoRestRMSAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ComboController : ControllerBase
    {
        private readonly IComboService _comboService;

        public ComboController(IComboService comboService)
        {
            _comboService = comboService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ComboDTO>>> GetCombo()
        {
            try
            {
                var combo = await _comboService.GetAllCombo();
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
