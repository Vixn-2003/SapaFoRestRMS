using BusinessAccessLayer.Services.Interfaces;
using DataAccessLayer.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace SapaFoRestRMSAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StatisticsController : ControllerBase
    {
        private readonly ICapacityStatisticsService _capacityStatisticsService;

        public StatisticsController(ICapacityStatisticsService capacityStatisticsService)
        {
            _capacityStatisticsService = capacityStatisticsService;
        }

        /// <summary>
        /// Thống kê sức chứa nhà hàng theo ngày + từng ca
        /// </summary>
        /// <param name="date">Ngày cần xem, nếu null sẽ lấy Today</param>
        [HttpGet("capacity")]
        public async Task<ActionResult<DayCapacitySummaryDto>> GetDayCapacity([FromQuery] DateTime? date)
        {
            var result = await _capacityStatisticsService.GetDayCapacityAsync(date);
            return Ok(result);
        }
    }
}
