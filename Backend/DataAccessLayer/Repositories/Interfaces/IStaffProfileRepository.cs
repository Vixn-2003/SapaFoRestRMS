using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DomainAccessLayer.Models;

namespace DataAccessLayer.Repositories.Interfaces
{
    public interface IStaffProfileRepository
    {
        Task<List<User>> GetAllWithDetailsAsync(CancellationToken ct = default);
        Task<User?> GetWithDetailsAsync(int userId, CancellationToken ct = default);
        Task UpdateAsync(User user, CancellationToken ct = default);
    }
}


