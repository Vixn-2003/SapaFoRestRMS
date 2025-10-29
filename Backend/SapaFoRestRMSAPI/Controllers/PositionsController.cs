using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DomainAccessLayer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DataAccessLayer.Dbcontext;

namespace SapaFoRestRMSAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,Manager,Owner")]
    public class PositionsController : ControllerBase
    {
        private readonly SapaFoRestRmsContext _context;

        public PositionsController(SapaFoRestRmsContext context)
        {
            _context = context;
        }

        // GET: api/positions
        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken ct)
        {
            var list = await _context.Positions.AsNoTracking().ToListAsync(ct);
            return Ok(list);
        }

        // GET: api/positions/search?term=&status=&page=1&pageSize=10
        [HttpGet("search")]
        public async Task<IActionResult> Search(
            [FromQuery] string? term,
            [FromQuery] int? status,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            CancellationToken ct = default)
        {
            if (page < 1) page = 1;
            if (pageSize <= 0 || pageSize > 200) pageSize = 10;

            var query = _context.Positions.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(term))
            {
                var keyword = term.Trim();
                query = query.Where(p => p.PositionName.Contains(keyword) || (p.Description != null && p.Description.Contains(keyword)));
            }

            if (status.HasValue)
            {
                query = query.Where(p => p.Status == status.Value);
            }

            var totalCount = await query.CountAsync(ct);
            var items = await query
                .OrderBy(p => p.PositionName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return Ok(new
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            });
        }

        // GET: api/positions/5
        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id, CancellationToken ct)
        {
            var pos = await _context.Positions.FindAsync(new object?[] { id }, ct);
            if (pos == null) return NotFound();
            return Ok(pos);
        }

        // POST: api/positions
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Position create, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(create.PositionName))
            {
                return BadRequest("PositionName is required");
            }

            var exists = await _context.Positions.AnyAsync(p => p.PositionName == create.PositionName, ct);
            if (exists)
            {
                return Conflict("Position with the same name already exists");
            }

            // Default active if not set
            if (create.Status == 0 || create.Status == 1 || create.Status == 2)
            {
                // leave as provided
            }
            else
            {
                create.Status = 0;
            }

            await _context.Positions.AddAsync(create, ct);
            await _context.SaveChangesAsync(ct);
            return CreatedAtAction(nameof(Get), new { id = create.PositionId }, create);
        }

        // PUT: api/positions/5
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] Position update, CancellationToken ct)
        {
            var pos = await _context.Positions.FindAsync(new object?[] { id }, ct);
            if (pos == null) return NotFound();

            // Check duplicate name if name changed
            if (!string.Equals(pos.PositionName, update.PositionName, StringComparison.OrdinalIgnoreCase))
            {
                var nameTaken = await _context.Positions.AnyAsync(p => p.PositionName == update.PositionName && p.PositionId != id, ct);
                if (nameTaken)
                {
                    return Conflict("Position with the same name already exists");
                }
            }

            pos.PositionName = update.PositionName;
            pos.Description = update.Description;
            pos.Status = update.Status;

            _context.Positions.Update(pos);
            await _context.SaveChangesAsync(ct);
            return NoContent();
        }

        // DELETE: api/positions/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var pos = await _context.Positions.FindAsync(new object?[] { id }, ct);
            if (pos == null) return NotFound();

            _context.Positions.Remove(pos);
            await _context.SaveChangesAsync(ct);
            return NoContent();
        }

        // PATCH: api/positions/5/status/1
        [HttpPatch("{id:int}/status/{status:int}")]
        public async Task<IActionResult> ChangeStatus(int id, int status, CancellationToken ct)
        {
            if (status < 0 || status > 2) return BadRequest("Invalid status");
            var pos = await _context.Positions.FindAsync(new object?[] { id }, ct);
            if (pos == null) return NotFound();
            pos.Status = status;
            _context.Positions.Update(pos);
            await _context.SaveChangesAsync(ct);
            return NoContent();
        }
    }
}


