using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessAccessLayer.DTOs.ShiftManagement;

namespace BusinessAccessLayer.Services.Interfaces;

/// <summary>
/// Interface cho Shift Management Service
/// </summary>
public interface IShiftManagementService
{
    /// <summary>
    /// Lấy ca làm việc hiện tại của staff
    /// </summary>
    Task<ShiftDashboardDto?> GetCurrentShiftAsync(int staffId, CancellationToken ct = default);

    /// <summary>
    /// Mở ca làm việc mới
    /// </summary>
    Task<ShiftResponseDto> OpenShiftAsync(OpenShiftRequestDto request, CancellationToken ct = default);

    /// <summary>
    /// Kết ca làm việc
    /// </summary>
    Task<ShiftResponseDto> CloseShiftAsync(CloseShiftRequestDto request, CancellationToken ct = default);

    /// <summary>
    /// Giao ca cho nhân viên khác
    /// </summary>
    Task<ShiftResponseDto> HandoverShiftAsync(HandoverShiftRequestDto request, CancellationToken ct = default);

    /// <summary>
    /// Lấy lịch sử ca làm việc
    /// </summary>
    Task<List<ShiftDto>> GetShiftHistoryAsync(int staffId, DateOnly fromDate, DateOnly toDate, CancellationToken ct = default);

    /// <summary>
    /// Lấy chi tiết ca làm việc
    /// </summary>
    Task<ShiftDto?> GetShiftDetailsAsync(int shiftId, CancellationToken ct = default);

    /// <summary>
    /// Kiểm tra xem staff có ca nào đang mở không
    /// </summary>
    Task<bool> HasOpenShiftAsync(int staffId, CancellationToken ct = default);
}

