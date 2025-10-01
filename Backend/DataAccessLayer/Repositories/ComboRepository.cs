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
    public class ComboRepository : IComboRepository
    {
        private readonly SapaFoRestRmsContext _context;

        public ComboRepository(SapaFoRestRmsContext context)
        {
            _context = context;
        }

        public Task CreateAsync(Combo entity)
        {
            throw new NotImplementedException();
        }

        public Task DeleteByIdAsync(int id)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Combo>> GetAllAsync()
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<Combo>> GetAllCombos()
        {
            try
            {
                return await _context.Combos
                    .Where(c => c.IsAvailable == true)
                    .Include(c => c.ComboItems)
                        .ThenInclude(ci => ci.MenuItem)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] {ex.Message}");
                throw;
            }
        }


        public Task<Combo> GetByIdAsync(int id)
        {
            throw new NotImplementedException();
        }

        public Task UpdateAsync(Combo entity)
        {
            throw new NotImplementedException();
        }
    }
}
