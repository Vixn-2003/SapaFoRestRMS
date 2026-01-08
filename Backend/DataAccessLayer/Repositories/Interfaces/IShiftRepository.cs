
using DomainAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories.Interfaces
{
    public interface IShiftRepository
    {
        Task<IEnumerable<Shift>> GetAllAsync();
        Task<Shift?> GetByIdAsync(int id);
        Task AddAsync(Shift shift);
        void Update(Shift shift);
        void Delete(Shift shift);
        Task<bool> SaveChangesAsync();

        Task<bool> IsConflictAsync(int departmentId, DateTime date, TimeSpan start, TimeSpan end, int? excludeId = null);
    }

}
