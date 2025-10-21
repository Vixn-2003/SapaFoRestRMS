using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BusinessAccessLayer.Services.Interfaces;
using DataAccessLayer.Dbcontext;
using DataAccessLayer.Repositories.Interfaces;
using DomainAccessLayer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace SapaFoRestRMSAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,Manager")]
    public class UsersController : ControllerBase
    {
        private readonly IUserRepository _users;
        private readonly SapaFoRestRmsContext _context;

        public UsersController(IUserRepository users, SapaFoRestRmsContext context)
        {
            _users = users;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken ct)
        {
            var list = await _context.Users.Where(u => u.IsDeleted == false).ToListAsync(ct);
            return Ok(list);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id)
        {
            var user = await _users.GetByIdAsync(id);
            if (user == null || user.IsDeleted == true) return NotFound();
            return Ok(user);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] User user, CancellationToken ct)
        {
            await _users.AddAsync(user);
            await _context.SaveChangesAsync(ct);
            return CreatedAtAction(nameof(Get), new { id = user.UserId }, user);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] User update, CancellationToken ct)
        {
            var user = await _users.GetByIdAsync(id);
            if (user == null || user.IsDeleted == true) return NotFound();
            user.FullName = update.FullName;
            user.Email = update.Email;
            user.Phone = update.Phone;
            user.Status = update.Status;
            user.RoleId = update.RoleId;
            await _users.UpdateAsync(user);
            await _context.SaveChangesAsync(ct);
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var user = await _users.GetByIdAsync(id);
            if (user == null || user.IsDeleted == true) return NotFound();
            user.IsDeleted = true;
            await _users.UpdateAsync(user);
            await _context.SaveChangesAsync(ct);
            return NoContent();
        }

        [HttpPatch("{id:int}/status/{status:int}")]
        public async Task<IActionResult> ChangeStatus(int id, int status, CancellationToken ct)
        {
            var user = await _users.GetByIdAsync(id);
            if (user == null || user.IsDeleted == true) return NotFound();
            user.Status = status;
            await _users.UpdateAsync(user);
            await _context.SaveChangesAsync(ct);
            return NoContent();
        }
    }
}


