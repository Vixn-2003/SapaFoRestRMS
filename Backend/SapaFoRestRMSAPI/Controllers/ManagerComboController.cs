using BusinessAccessLayer.DTOs.ManagementCombo;
using BusinessAccessLayer.DTOs.Manager;
using BusinessAccessLayer.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using static BusinessAccessLayer.DTOs.ManagementCombo.UpdateDtosCombo;

namespace SapaFoRestRMSAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ManagerComboController : ControllerBase
    {
        private readonly IManagerComboService _managerComboService;
        private readonly ICloudinaryService _cloudinaryService;

        public ManagerComboController(IManagerComboService comboService, ICloudinaryService cloudinaryService)
        {
            _managerComboService = comboService;
            _cloudinaryService = cloudinaryService;
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
            var result = _managerComboService.GetComboDisplayList(null, true, 1, int.MaxValue);
            var combos = result.Items;

            var topCombos = period.ToLower() switch
            {
                "month" => combos
                            .Where(c => c.MonthlyUsed > 0)   // chỉ lấy combo bán được
                            .OrderByDescending(c => c.MonthlyUsed)
                            .Take(3),
                _ => combos
                        .Where(c => c.WeeklyUsed > 0)
                        .OrderByDescending(c => c.WeeklyUsed)
                        .Take(3),
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
                "month" => combos
                            .OrderBy(c => c.MonthlyUsed)   // sắp xếp từ ít bán nhất
                            .Take(Math.Min(3, combos.Count)), // lấy tối đa 3 combo, nếu combo <3 thì lấy hết
                _ => combos
                        .OrderBy(c => c.WeeklyUsed)
                        .Take(Math.Min(3, combos.Count)),
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

        // GET: api/combos/5
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var result = await _managerComboService.GetByIdAsync(id);
            return Ok(result);
        }

        // GET: api/combos/menu?keyword=abc
        [HttpGet("AllMenu")]
        public async Task<IActionResult> Get(
    [FromQuery] string? keyword,
    [FromQuery] string? categoryName,
    [FromQuery] int pageIndex = 1)
        {
            try
            {
                var result = await _managerComboService.SearchAsync(keyword, categoryName, pageIndex);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }


        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromForm] UpdateComboDto request)
        {
            try
            {
                // 1. Nếu có file ảnh mới → Upload lên Cloudinary
                if (request.ImageFile != null && request.ImageFile.Length > 0)
                {
                    var newImageUrl = await _cloudinaryService.UploadImageAsync(
                        request.ImageFile,
                        "combos"
                    );

                    if (string.IsNullOrEmpty(newImageUrl))
                    {
                        return BadRequest(new { message = "❌ Upload ảnh thất bại" });
                    }

                    // 2. Xóa ảnh cũ (nếu có)
                    if (!string.IsNullOrEmpty(request.ImageUrl))
                    {
                        await _cloudinaryService.DeleteImageAsync(request.ImageUrl);
                    }

                    // 3. Gán URL ảnh mới
                    request.ImageUrl = newImageUrl;
                }
                // Nếu không có file mới → giữ nguyên ImageUrl cũ (đã có trong request.ImageUrl)

                // 4. Cập nhật combo
                await _managerComboService.UpdateAsync(id, request);

                return Ok(new { message = "✅ Cập nhật combo thành công" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    errorCode = "COMBO_IN_USE_OR_UNAVAILABLE_ITEM",
                    message = ex.Message
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    errorCode = "INVALID_REQUEST",
                    message = ex.Message
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new
                {
                    errorCode = "COMBO_NOT_FOUND",
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    errorCode = "INTERNAL_SERVER_ERROR",
                    message = $"Có lỗi xảy ra: {ex.Message}"
                });
            }
        }



        [HttpPost("CreateCombo")]
        public async Task<IActionResult> Create([FromForm] CreateComboDto request)
        {
            try
            {
                // 1. Upload ảnh lên Cloudinary (nếu có)
                if (request.ImageFile != null && request.ImageFile.Length > 0)
                {
                    var imageUrl = await _cloudinaryService.UploadImageAsync(
                        request.ImageFile,
                        "combos" // Folder trên Cloudinary
                    );

                    if (string.IsNullOrEmpty(imageUrl))
                    {
                        return BadRequest(new { message = "❌ Upload ảnh thất bại" });
                    }

                    // Gán URL vào DTO để lưu DB
                    request.ImageUrl = imageUrl;
                }

                // 2. Tạo combo (Service sẽ lưu ImageUrl vào DB)
                await _managerComboService.AddComboAsync(request);

                return Ok(new { message = "Tạo combo thành công!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"❌ Lỗi: {ex.Message}" });
            }
        }
        [HttpGet("top5new")]
        public async Task<IActionResult> GetTop5NewMenuItems()
        {
            try
            {
                var top5MenuItems = await _managerComboService.GetTop5NewMenuItemsAsync();

                // Nếu muốn trả về PagedResult hoặc metadata khác, có thể thêm sau
                return Ok(top5MenuItems);
            }
            catch (Exception ex)
            {
                // Log nếu cần
                return StatusCode(500, new { message = "Lỗi khi lấy menu mới nhất.", details = ex.Message });
            }
        }
    }
}
