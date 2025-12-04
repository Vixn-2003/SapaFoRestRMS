using DataAccessLayer.Dbcontext;
using DataAccessLayer.Repositories.Interfaces;
using DomainAccessLayer.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories
{
    public class ShiftTemplateRepository : IShiftTemplateRepository
    {
        private readonly SapaFoRestRmsContext _context;

        public ShiftTemplateRepository(SapaFoRestRmsContext context)
        {
            _context = context;
        }

        public async Task AddAsync(ShiftTemplate entity)
        {
            await _context.ShiftTemplates.AddAsync(entity);
        }

        public void Delete(ShiftTemplate entity)
        {
            _context.ShiftTemplates.Remove(entity);
        }

        public async Task<List<ShiftTemplate>> GetAllAsync()
        {
            return await _context.ShiftTemplates
                .Include(x => x.DayType)
                .Include(x => x.Department)
                .ToListAsync();
        }

        public async Task<ShiftTemplate?> GetByIdAsync(int id)
        {
            return await _context.ShiftTemplates
                .Include(x => x.DayType)
                .Include(x => x.Department)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<bool> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }

        public void Update(ShiftTemplate entity)
        {
            _context.ShiftTemplates.Update(entity);
        }

        public async Task<bool> DayTypeExistsAsync(int dayTypeId)
        {
            return await _context.DayTypes.AnyAsync(x => x.Id == dayTypeId);
        }

        public async Task<bool> DepartmentExistsAsync(int deptId)
        {
            return await _context.Departments.AnyAsync(x => x.DepartmentId == deptId);
        }

        public async Task<bool> CodeExistsAsync(string code, int? excludeId = null)
        {
            return await _context.ShiftTemplates
                .AnyAsync(x => x.Code == code && (excludeId == null || x.Id != excludeId));
        }
    }
}
