using BusinessAccessLayer.DTOs.ManagementCombo;
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

        [HttpGet("menu")]
        public async Task<IActionResult> GetMenu([FromQuery] MenuFilterRequest filter)
        {
            var result = await _managerComboService.GetMenuItemsAsync(filter);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> CreateCombo([FromBody] CreateComboRequest request)
        {
            await _managerComboService.CreateComboAsync(request);
            return Ok(new { message = "Combo created successfully" });
        }

        [HttpGet("top-sellers")]
        public async Task<IActionResult> GetTopSellers([FromQuery] string type) // type = "menu" or "combo"
        {
            var result = await _managerComboService.GetTopSellersAsync(type);
            return Ok(result);
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats([FromQuery] string timeFrame) // timeFrame = "week", "month", "year"
        {
            var result = await _managerComboService.GetComboSalesStatsAsync(timeFrame);
            return Ok(result);
        }

        [HttpGet("GetListCombo")]
        public IActionResult GetCombos([FromQuery] string? search, [FromQuery] bool? isAvailable,
                                             int pageIndex = 1, int pageSize = 5)
        {
            var result = _managerComboService.GetComboDisplayList(search, isAvailable, pageIndex, pageSize);
            return Ok(result);
        }
        [HttpGet("api/combo/top")]
        public IActionResult GetTopCombos(string period = "week")
        {
            // Lấy hết combo (có thể pageSize rất lớn hoặc implement thêm GetAll)
            var result = _managerComboService.GetComboDisplayList(null, true, 1, int.MaxValue);
            var combos = result.Items;

            var topCombos = period.ToLower() switch
            {
                "month" => combos.OrderByDescending(c => c.MonthlyUsed).Take(3),
                _ => combos.OrderByDescending(c => c.WeeklyUsed).Take(3),
            };

            return Ok(topCombos);
        }

        [HttpGet("api/combo/low")]
        public IActionResult GetLowCombos(string period = "week")
        {
            var result = _managerComboService.GetComboDisplayList(null, true, 1, int.MaxValue);
            var combos = result.Items;

            var lowCombos = period.ToLower() switch
            {
                "month" => combos.OrderBy(c => c.MonthlyUsed).Take(3),
                _ => combos.OrderBy(c => c.WeeklyUsed).Take(3),
            };

            return Ok(lowCombos);
        }

        [HttpGet("api/combo/overview")]
        public IActionResult GetComboOverview()
        {
            var result = _managerComboService.GetComboDisplayList(null, true, 1, int.MaxValue);
            var combos = result.Items;

            var overview = new
            {
                TotalActiveCombos = combos.Count(),
                TotalOrdersWeek = combos.Sum(c => c.WeeklyUsed),
                TotalOrdersMonth = combos.Sum(c => c.MonthlyUsed)
            };

            return Ok(overview);
        }

        [HttpGet("GetComboBy/{id}")]
        public async Task<IActionResult> GetComboDetail(int id)
        {
            try
            {
                // Hàm này trả về ComboDetailDto chuẩn chỉnh
                var result = await _managerComboService.GetComboByIdAsync(id);
                return Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCombo(int id, [FromBody] UpdateComboRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                await _managerComboService.UpdateComboAsync(id, request);
                return Ok(new { message = "Cập nhật Combo thành công!" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                // Log error
                return StatusCode(500, new { message = "Lỗi hệ thống: " + ex.Message });
            }
        }
    }
}
