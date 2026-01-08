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
    public class RestaurantIntroRepository : IRestaurantIntroRepository
    {
        private readonly SapaFoRestRmsContext _context;

        public RestaurantIntroRepository(SapaFoRestRmsContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<RestaurantIntro>> GetAllAsync()
        {
            return await _context.RestaurantIntros.OrderByDescending(x => x.CreatedDate).ToListAsync();
        }

        public async Task<IEnumerable<RestaurantIntro>> GetActiveAsync()
        {
            return await _context.RestaurantIntros
                .Where(x => x.IsActive == true)
                .OrderByDescending(x => x.CreatedDate)
                .ToListAsync();
        }

        public async Task<RestaurantIntro?> GetByIdAsync(int id)
        {
            return await _context.RestaurantIntros.FindAsync(id);
        }

        public async Task AddAsync(RestaurantIntro intro)
        {
            await _context.RestaurantIntros.AddAsync(intro);
        }

        public async Task UpdateAsync(RestaurantIntro intro)
        {
            _context.RestaurantIntros.Update(intro);
        }

        public async Task DeleteAsync(RestaurantIntro intro)
        {
            _context.RestaurantIntros.Remove(intro);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
