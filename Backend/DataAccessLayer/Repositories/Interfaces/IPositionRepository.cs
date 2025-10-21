using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DomainAccessLayer.Models;

namespace DataAccessLayer.Repositories.Interfaces
{
    public interface IPositionRepository
    {
        Task<List<Position>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken ct = default);
    }
}


