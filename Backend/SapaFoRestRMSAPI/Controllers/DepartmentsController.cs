using DataAccessLayer.Dbcontext;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace SapaFoRestRMSAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DepartmentsController : ControllerBase
    {
        private readonly SapaFoRestRmsContext _context;

        public DepartmentsController(SapaFoRestRmsContext context)
        {
            _context = context;
        }

        // GET: api/Departments
        [HttpGet]
        public async Task<IActionResult> GetDepartments()
        {
            var deps = await _context.Departments
                .Where(d => d.Status == 1)   // chỉ lấy bộ phận đang hoạt động (nếu bạn muốn)
                .Select(d => new
                {
                    d.DepartmentId,
                    d.Name,
                    d.Status
                })
                .ToListAsync();

            return Ok(deps);
        }
    }
}
