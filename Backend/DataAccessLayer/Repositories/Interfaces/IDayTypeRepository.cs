using DomainAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories.Interfaces
{
    public interface IDayTypeRepository
    {
        Task<List<DayType>> GetAllAsync();
        Task<DayType?> GetByIdAsync(int id);
        Task AddAsync(DayType entity);
        void Update(DayType entity);
        void Delete(DayType entity);
        Task<bool> SaveChangesAsync();
        Task<bool> ExistsByNameAsync(string name);
    }
}
