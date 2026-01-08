using DomainAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories.Interfaces
{
    public interface IUnitRepository
    {
        Task<IEnumerable<Unit>> GetAllUnits(); 
        Task<int> GetIdUnitByString(string unitName);

        Task<bool> ExistsByNameAsync(string unitName, int? excludeId = null);
        Task AddAsync(Unit unit);
        Task UpdateAsync(Unit unit);

        Task<Unit?> GetByIdAsync(int id);
    }
}
