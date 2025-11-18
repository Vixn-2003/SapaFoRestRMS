using BusinessAccessLayer.DTOs.Positions;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BusinessAccessLayer.Services.Interfaces;

/// <summary>
/// Interface cho SalaryChangeRequest Service
/// </summary>
public interface ISalaryChangeRequestService
{
    /// <summary>
    /// Manager: Tạo yêu cầu thay đổi lương
    /// </summary>
    Task<SalaryChangeRequestDto> CreateRequestAsync(CreateSalaryChangeRequestDto request, int requestedByUserId, CancellationToken ct = default);

    /// <summary>
    /// Owner: Lấy danh sách yêu cầu chờ phê duyệt
    /// </summary>
    Task<IEnumerable<SalaryChangeRequestDto>> GetPendingRequestsAsync(CancellationToken ct = default);

    /// <summary>
    /// Owner: Lấy tất cả yêu cầu
    /// </summary>
    Task<IEnumerable<SalaryChangeRequestDto>> GetAllRequestsAsync(string? status = null, CancellationToken ct = default);

    /// <summary>
    /// Owner: Phê duyệt hoặc từ chối yêu cầu
    /// </summary>
    Task<SalaryChangeRequestDto> ReviewRequestAsync(ReviewSalaryChangeRequestDto request, int approvedByUserId, CancellationToken ct = default);

    /// <summary>
    /// Manager: Lấy danh sách yêu cầu của mình
    /// </summary>
    Task<IEnumerable<SalaryChangeRequestDto>> GetMyRequestsAsync(int userId, CancellationToken ct = default);

    /// <summary>
    /// Owner: Lấy thống kê yêu cầu thay đổi lương
    /// </summary>
    Task<SalaryChangeRequestStatisticsDto> GetStatisticsAsync(CancellationToken ct = default);

    /// <summary>
    /// Lấy chi tiết yêu cầu
    /// </summary>
    Task<SalaryChangeRequestDto?> GetByIdAsync(int requestId, CancellationToken ct = default);
}

