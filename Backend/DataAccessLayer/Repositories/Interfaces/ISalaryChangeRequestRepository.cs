using DomainAccessLayer.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories.Interfaces;

/// <summary>
/// Interface cho SalaryChangeRequest Repository
/// </summary>
public interface ISalaryChangeRequestRepository : IRepository<SalaryChangeRequest>
{
    /// <summary>
    /// Lấy danh sách yêu cầu theo trạng thái
    /// </summary>
    Task<IEnumerable<SalaryChangeRequest>> GetByStatusAsync(string status);

    /// <summary>
    /// Lấy danh sách yêu cầu chờ phê duyệt (Pending)
    /// </summary>
    Task<IEnumerable<SalaryChangeRequest>> GetPendingRequestsAsync();

    /// <summary>
    /// Lấy yêu cầu kèm thông tin Position và User
    /// </summary>
    Task<SalaryChangeRequest?> GetByIdWithDetailsAsync(int requestId);

    /// <summary>
    /// Lấy danh sách yêu cầu theo PositionId
    /// </summary>
    Task<IEnumerable<SalaryChangeRequest>> GetByPositionIdAsync(int positionId);

    /// <summary>
    /// Lấy danh sách yêu cầu theo người tạo
    /// </summary>
    Task<IEnumerable<SalaryChangeRequest>> GetByRequestedByAsync(int userId);
}

