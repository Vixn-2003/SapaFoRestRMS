using BusinessAccessLayer.DTOs;
using BusinessAccessLayer.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace SapaFoRestRMSAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PayrollController : ControllerBase
    {
        private readonly IPayrollService _payrollService;

        public PayrollController(IPayrollService payrollService)
        {
            _payrollService = payrollService;
        }

        //  GET: api/Payroll
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _payrollService.GetAllAsync();
            return Ok(result);
        }

        //  GET: api/Payroll/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var payroll = await _payrollService.GetByIdAsync(id);
            if (payroll == null)
                return NotFound(new { message = "Payroll not found" });

            return Ok(payroll);
        }

        //  POST: api/Payroll
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] PayrollDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            await _payrollService.AddAsync(dto);
            return Ok(new { message = "Payroll created successfully" });
        }

        // ✅ PUT: api/Payroll/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] PayrollDTO dto)
        {
            if (id != dto.PayrollId)
                return BadRequest(new { message = "Payroll ID mismatch" });

            var existing = await _payrollService.GetByIdAsync(id);
            if (existing == null)
                return NotFound(new { message = "Payroll not found" });

            await _payrollService.UpdateAsync(dto);
            return Ok(new { message = "Payroll updated successfully" });
        }

        //  DELETE: api/Payroll/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var existing = await _payrollService.GetByIdAsync(id);
            if (existing == null)
                return NotFound(new { message = "Payroll not found" });

            await _payrollService.DeleteAsync(id);
            return Ok(new { message = "Payroll deleted successfully" });
        }

        // ✅ SEARCH: api/Payroll/search?name=...
        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string name)
        {
            var result = await _payrollService.SearchAsync(name);
            return Ok(result);
        }

        // ✅ FILTER: api/Payroll/filter?... (lọc và sắp xếp)
        [HttpGet("filter")]
        public async Task<IActionResult> Filter(
            [FromQuery] string? sortBy,
            [FromQuery] bool descending = false,
            [FromQuery] decimal? minBaseSalary = null,
            [FromQuery] decimal? maxBaseSalary = null,
            [FromQuery] int? minWorkDays = null,
            [FromQuery] int? maxWorkDays = null,
            [FromQuery] decimal? minBonus = null,
            [FromQuery] decimal? maxBonus = null,
            [FromQuery] decimal? minPenalty = null,
            [FromQuery] decimal? maxPenalty = null,
            [FromQuery] decimal? minNetSalary = null,
            [FromQuery] decimal? maxNetSalary = null,
            [FromQuery] string? monthYear = null)
        {
            var result = await _payrollService.FilterAsync(
                sortBy,
                descending,
                minBaseSalary,
                maxBaseSalary,
                minWorkDays,
                maxWorkDays,
                minBonus,
                maxBonus,
                minPenalty,
                maxPenalty,
                minNetSalary,
                maxNetSalary,
                monthYear);

            return Ok(result);
        }

        // ✅ PAGINATION: api/Payroll/paged?pageNumber=1&pageSize=10&name=...
        [HttpGet("paged")]
        public async Task<IActionResult> GetPaged(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? name = null,
            [FromQuery] string? sortBy = null,
            [FromQuery] bool descending = false)
        {
            var (data, total) = await _payrollService.GetPagedAsync(pageNumber, pageSize, name, sortBy, descending);

            return Ok(new
            {
                totalRecords = total,
                pageNumber,
                pageSize,
                data
            });
        }
    }
}
