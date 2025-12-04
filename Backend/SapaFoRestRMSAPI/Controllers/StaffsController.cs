using BusinessAccessLayer.DTOs;
using DataAccessLayer.Dbcontext;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;

namespace SapaFoRestRMSAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StaffsController : ControllerBase
    {
        private readonly SapaFoRestRmsContext _context;
        public StaffsController(SapaFoRestRmsContext context)
        {
            _context = context;
        }

        // GET: api/Staffs
        [HttpGet]
        public async Task<ActionResult<IEnumerable<StaffDTO>>> GetStaffs()
        {
            var staffs = await _context.Staffs
                .Include(s => s.User)
                .Where(s => s.Status == 1) // chỉ lấy active
                .ToListAsync();

            var dto = staffs.Select(s => new StaffDTO
            {
                StaffId = s.StaffId,
                FullName = s.User.FullName,
                Phone = s.User.Phone,
                SalaryBase = s.SalaryBase,
                Status = s.Status
            }).ToList();

            return dto;
        }
    }
}
