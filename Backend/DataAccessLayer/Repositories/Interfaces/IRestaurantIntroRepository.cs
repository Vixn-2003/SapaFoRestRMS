using DomainAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories.Interfaces
{
    public interface IRestaurantIntroRepository
    {
        Task<IEnumerable<RestaurantIntro>> GetAllAsync();
        Task<IEnumerable<RestaurantIntro>> GetActiveAsync();
        Task<RestaurantIntro?> GetByIdAsync(int id);
        Task AddAsync(RestaurantIntro intro);
        Task UpdateAsync(RestaurantIntro intro);
        Task DeleteAsync(RestaurantIntro intro);
        Task SaveChangesAsync();
    }
}
