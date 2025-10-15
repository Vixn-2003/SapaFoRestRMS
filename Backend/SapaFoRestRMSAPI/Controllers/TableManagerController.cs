using BusinessAccessLayer.DTOs;
using BusinessAccessLayer.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace SapaFoRestRMSAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TableManagerController : ControllerBase
    {
        private readonly ITableService _tableService;

        public TableManagerController(ITableService tableService)
        {
            _tableService = tableService;
        }

        [HttpGet]
        public async Task<IActionResult> GetTables(
            [FromQuery] string? search,
            [FromQuery] int? capacity,
            [FromQuery] int? areaId,
            [FromQuery] string? status ,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var (tables, totalCount) = await _tableService.GetTablesAsync(search, capacity, areaId, page, pageSize,status);
            return Ok(new { totalCount, data = tables });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetTable(int id)
        {
            var table = await _tableService.GetByIdAsync(id);
            if (table == null) return NotFound();
            return Ok(table);
        }

        [HttpPost]
        public async Task<IActionResult> AddTable([FromBody] TableManageDto dto)
        {
            await _tableService.AddAsync(dto);
            return Ok(new { message = "Table added successfully" });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTable(int id, [FromBody] TableManageDto dto)
        {
            await _tableService.UpdateAsync(id, dto);
            return Ok(new { message = "Table updated successfully" });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _tableService.DeleteAsync(id);
            if (!result.Success)
                return BadRequest(result.Message);
            return Ok(result.Message);
        }

    }
}
