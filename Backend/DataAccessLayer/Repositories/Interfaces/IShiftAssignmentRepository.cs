using DomainAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories.Interfaces
{
    public interface IShiftAssignmentRepository
    {
        Task<IEnumerable<ShiftAssignment>> GetAllAsync();
        Task<ShiftAssignment?> GetByIdAsync(int id);
        Task AddAsync(ShiftAssignment assignment);
        void Update(ShiftAssignment assignment);
        void Delete(ShiftAssignment assignment);
        Task<bool> SaveChangesAsync();

        // Check nếu nhân viên đã được phân ca trùng giờ
        Task<bool> IsConflictAsync(int staffId, DateTime date, TimeSpan start, TimeSpan end, int? excludeId = null);
    }
}
