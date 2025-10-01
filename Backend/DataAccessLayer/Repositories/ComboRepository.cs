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

        public async Task<List<Combo>> GetAllAsync()
        {
            return await _context.Combos
                .Where(c => c.IsAvailable == true || c.IsAvailable == null)
                .ToListAsync();
        }
    }
}
