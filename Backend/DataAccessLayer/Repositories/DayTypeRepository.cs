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
    public class DayTypeRepository : IDayTypeRepository
    {
        private readonly SapaFoRestRmsContext _context;

        public DayTypeRepository(SapaFoRestRmsContext context)
        {
            _context = context;
        }

        public async Task<List<DayType>> GetAllAsync()
        {
            return await _context.DayTypes.ToListAsync();
        }

        public async Task<DayType?> GetByIdAsync(int id)
        {
            return await _context.DayTypes.FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task AddAsync(DayType entity)
        {
            await _context.DayTypes.AddAsync(entity);
        }

        public void Update(DayType entity)
        {
            _context.DayTypes.Update(entity);
        }

        public void Delete(DayType entity)
        {
            _context.DayTypes.Remove(entity);
        }

        public async Task<bool> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> ExistsByNameAsync(string name)
        {
            return await _context.DayTypes.AnyAsync(x => x.Name == name);
        }
    }
}
