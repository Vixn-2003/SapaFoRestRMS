using DomainAccessLayer.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories.Interfaces
{
    public interface IPositionRepository : IRepository<Position>
    {
        Task<List<Position>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken ct = default);
        Task<bool> IsNameExistsAsync(string positionName, CancellationToken ct = default);
        Task<bool> IsNameExistsAsync(string positionName, int excludeId, CancellationToken ct = default);
        Task<List<Position>> SearchAsync(string? searchTerm, int? status, CancellationToken ct = default);
        Task<(List<Position> Items, int TotalCount)> SearchWithPaginationAsync(
            string? searchTerm, 
            int? status, 
            int page, 
            int pageSize, 
            CancellationToken ct = default);
    }
}


