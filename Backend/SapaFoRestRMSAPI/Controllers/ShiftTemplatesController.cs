using BusinessAccessLayer.DTOs;
using DataAccessLayer.Dbcontext;
using DomainAccessLayer.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;

namespace SapaFoRestRMSAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ShiftTemplatesController : ControllerBase
    {
        private readonly SapaFoRestRmsContext _context;
        public ShiftTemplatesController(SapaFoRestRmsContext context) => _context = context;

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ShiftTemplateDTO>>> GetTemplates()
        {
            var templates = await _context.ShiftTemplates.Include(t => t.Department).ToListAsync();
            var dto = templates.Select(t => new ShiftTemplateDTO
            {
                TemplateId = t.ShiftTemplateId,
                Name = t.Name,
                Start = t.Start,
                End = t.End,
                ShiftType = t.ShiftType,
                DepartmentId = t.DepartmentId,
                DepartmentName = t.Department.Name
            }).ToList();
            return dto;
        }

        [HttpPost]
        public async Task<ActionResult<ShiftTemplateDTO>> CreateTemplate(ShiftTemplateCreateDTO dto)
        {
            var template = new ShiftTemplate
            {
                Name = dto.Name,
                Start = dto.Start,
                End = dto.End,
                ShiftType = dto.ShiftType,
                DepartmentId = dto.DepartmentId
            };
            _context.ShiftTemplates.Add(template);
            await _context.SaveChangesAsync();

            var result = new ShiftTemplateDTO
            {
                TemplateId = template.ShiftTemplateId,
                Name = template.Name,
                Start = template.Start,
                End = template.End,
                ShiftType = template.ShiftType,
                DepartmentId = template.DepartmentId,
                DepartmentName = (await _context.Departments.FindAsync(template.DepartmentId))?.Name ?? ""
            };

            return CreatedAtAction(nameof(GetTemplates), new { id = template.ShiftTemplateId }, result);
        }
    }
}
