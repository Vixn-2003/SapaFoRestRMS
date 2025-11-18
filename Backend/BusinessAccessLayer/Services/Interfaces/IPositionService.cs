using BusinessAccessLayer.DTOs.Positions;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BusinessAccessLayer.Services.Interfaces
{
    public interface IPositionService
    {
        Task<IEnumerable<PositionDto>> GetAllAsync(CancellationToken ct = default);
        Task<PositionDto?> GetByIdAsync(int id, CancellationToken ct = default);
        Task<PositionListResponse> SearchAsync(PositionSearchRequest request, CancellationToken ct = default);
        Task<PositionDto> CreateAsync(PositionCreateRequest request, CancellationToken ct = default);
        Task UpdateAsync(int id, PositionUpdateRequest request, CancellationToken ct = default);
        Task DeleteAsync(int id, CancellationToken ct = default);
        Task ChangeStatusAsync(int id, int status, CancellationToken ct = default);
    }
}

