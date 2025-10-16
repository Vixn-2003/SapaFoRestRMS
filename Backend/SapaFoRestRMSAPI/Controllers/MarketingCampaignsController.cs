using BusinessAccessLayer.DTOs;
using BusinessAccessLayer.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace SapaFoRestRMSAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MarketingCampaignsController : ControllerBase
    {
        private readonly IMarketingCampaignService _service;

        public MarketingCampaignsController(IMarketingCampaignService service)
        {
            _service = service;
        }

        // GET: api/MarketingCampaigns
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var campaigns = await _service.GetAllAsync();
            return Ok(campaigns);
        }

        // GET: api/MarketingCampaigns/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var campaign = await _service.GetByIdAsync(id);
            if (campaign == null)
                return NotFound(new { message = "Campaign not found" });

            return Ok(campaign);
        }

        // GET: api/MarketingCampaigns/list
        [HttpGet("list")]
        public async Task<IActionResult> GetList(
            [FromQuery] string? searchTerm,
            [FromQuery] string? campaignType,
            [FromQuery] string? status,
            [FromQuery] string? startDate,
            [FromQuery] string? endDate,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            DateOnly? parsedStartDate = null;
            DateOnly? parsedEndDate = null;

            if (!string.IsNullOrWhiteSpace(startDate) && DateOnly.TryParse(startDate, out var sd))
                parsedStartDate = sd;

            if (!string.IsNullOrWhiteSpace(endDate) && DateOnly.TryParse(endDate, out var ed))
                parsedEndDate = ed;

            var (data, totalCount) = await _service.SearchFilterPaginateAsync(
                searchTerm, campaignType, status, parsedStartDate, parsedEndDate, pageNumber, pageSize);

            return Ok(new
            {
                data,
                totalCount,
                pageNumber,
                pageSize,
                totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            });
        }

        // POST: api/MarketingCampaigns
        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Create([FromForm] MarketingCampaignCreateDto dto, IFormFile? imageFile)
        {
            try
            {
                var created = await _service.CreateAsync(dto, imageFile);
                return CreatedAtAction(nameof(GetById), new { id = created.CampaignId }, created);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // PUT: api/MarketingCampaigns/5
        [HttpPut("{id}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Update(int id, [FromForm] MarketingCampaignUpdateDto dto, IFormFile? imageFile)
        {
            try
            {
                var updated = await _service.UpdateAsync(id, dto, imageFile);
                if (updated == null)
                    return NotFound(new { message = "Campaign not found" });

                return Ok(updated);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // DELETE: api/MarketingCampaigns/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _service.DeleteAsync(id);
            if (!deleted)
                return NotFound(new { message = "Campaign not found" });

            return NoContent();
        }

        // GET: api/MarketingCampaigns/kpis
        [HttpGet("kpis")]
        public async Task<IActionResult> GetKpis(
            [FromQuery] string? startDate,
            [FromQuery] string? endDate)
        {
            DateOnly? parsedStartDate = null;
            DateOnly? parsedEndDate = null;

            if (!string.IsNullOrWhiteSpace(startDate) && DateOnly.TryParse(startDate, out var sd))
                parsedStartDate = sd;

            if (!string.IsNullOrWhiteSpace(endDate) && DateOnly.TryParse(endDate, out var ed))
                parsedEndDate = ed;

            var kpis = await _service.GetKpisAsync(parsedStartDate, parsedEndDate);
            return Ok(kpis);
        }

        // GET: api/MarketingCampaigns/charts/performance
        [HttpGet("charts/performance")]
        public async Task<IActionResult> GetPerformanceChart(
            [FromQuery] string startDate,
            [FromQuery] string endDate)
        {
            if (!DateOnly.TryParse(startDate, out var sd) || !DateOnly.TryParse(endDate, out var ed))
                return BadRequest(new { message = "Invalid date format" });

            var data = await _service.GetPerformanceChartDataAsync(sd, ed);
            return Ok(data);
        }

        // GET: api/MarketingCampaigns/charts/comparison
        [HttpGet("charts/comparison")]
        public async Task<IActionResult> GetYearOverYearComparison(
            [FromQuery] string startDate,
            [FromQuery] string endDate)
        {
            if (!DateOnly.TryParse(startDate, out var sd) || !DateOnly.TryParse(endDate, out var ed))
                return BadRequest(new { message = "Invalid date format" });

            var comparison = await _service.GetYearOverYearComparisonAsync(sd, ed);
            return Ok(comparison);
        }

        // GET: api/MarketingCampaigns/kpi/target
        [HttpGet("kpi/target")]
        public async Task<IActionResult> GetTargetKpi(
            [FromQuery] string startDate,
            [FromQuery] string endDate,
            [FromQuery] decimal targetRevenue,
            [FromQuery] int targetReach)
        {
            if (!DateOnly.TryParse(startDate, out var sd) || !DateOnly.TryParse(endDate, out var ed))
                return BadRequest(new { message = "Invalid date format" });

            var targetKpi = await _service.GetTargetKpiAsync(sd, ed, targetRevenue, targetReach);
            return Ok(targetKpi);
        }

        // GET: api/MarketingCampaigns/charts/distribution
        [HttpGet("charts/distribution")]
        public async Task<IActionResult> GetCampaignDistribution()
        {
            var distribution = await _service.GetCampaignDistributionAsync();
            return Ok(distribution);
        }
    }
}