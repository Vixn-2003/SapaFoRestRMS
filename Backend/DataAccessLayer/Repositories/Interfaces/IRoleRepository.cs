using DomainAccessLayer.Models;
using System.Threading;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories.Interfaces
{
    public interface IRoleRepository : IRepository<Role>
    {
        Task<Role?> GetByNameAsync(string roleName, CancellationToken ct = default);
        Task<List<Role>> GetAllOrderedAsync(CancellationToken ct = default);
    }
}

