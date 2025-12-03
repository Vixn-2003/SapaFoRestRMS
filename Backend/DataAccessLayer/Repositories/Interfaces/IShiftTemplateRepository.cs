using DomainAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories.Interfaces
{
    public interface IShiftTemplateRepository
    {
        Task<List<ShiftTemplate>> GetAllAsync();
        Task<ShiftTemplate?> GetByIdAsync(int id);
        Task AddAsync(ShiftTemplate entity);
        void Update(ShiftTemplate entity);
        void Delete(ShiftTemplate entity);
        Task<bool> SaveChangesAsync();

        Task<bool> DayTypeExistsAsync(int dayTypeId);
        Task<bool> DepartmentExistsAsync(int deptId);
        Task<bool> CodeExistsAsync(string code, int? excludeId = null);
    }
}
