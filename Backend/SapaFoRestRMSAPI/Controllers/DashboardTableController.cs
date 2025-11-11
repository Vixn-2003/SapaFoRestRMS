using BusinessAccessLayer.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace SapaFoRestRMSAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DashboardTableController : ControllerBase
    {
        private readonly IDashboardTableService _dashboardTableService;

        public DashboardTableController(IDashboardTableService dashboardTableService)
        {
            _dashboardTableService = dashboardTableService;
        }


        [HttpGet("List-Table")]
        public async Task<IActionResult> GetDashboardTableData([FromQuery] string? areaName,
        [FromQuery] int? floor,
        [FromQuery] string? status,
        [FromQuery] string? searchString,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12)
        {
            try
            {
                var data = await _dashboardTableService.GetDashboardDataAsync(areaName, floor, status, searchString, page, pageSize);
                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi máy chủ nội bộ: {ex.Message}" });
            }
        }

    }

}
