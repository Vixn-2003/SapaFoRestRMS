using BusinessAccessLayer.DTOs.ShiftManagement;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BusinessAccessLayer.Services.Interfaces;

/// <summary>
/// Service interface for Counter Staff Shift Management
/// </summary>
public interface IShiftManagementService
{
    // ========== OPENING SHIFT ==========
    /// <summary>
    /// UC125 - Declare Opening Balance
    /// </summary>
    Task<ShiftOpeningResponseDto> DeclareOpeningBalanceAsync(ShiftOpeningDeclareRequestDto request, CancellationToken ct = default);

    /// <summary>
    /// UC126 - Submit Opening Denominations
    /// </summary>
    Task<ShiftDenominationsResponseDto> SubmitOpeningDenominationsAsync(ShiftOpeningDenominationsRequestDto request, CancellationToken ct = default);

    /// <summary>
    /// UC127 - Confirm Shift Opening
    /// </summary>
    Task<ShiftOpeningResponseDto> ConfirmShiftOpeningAsync(ShiftOpeningConfirmRequestDto request, CancellationToken ct = default);

    // ========== CLOSING SHIFT ==========
    /// <summary>
    /// UC128 - Count Closing Cash (denominations)
    /// </summary>
    Task<ShiftDenominationsResponseDto> CountClosingCashAsync(ShiftDenominationsRequestDto request, CancellationToken ct = default);

    /// <summary>
    /// UC129 - Calculate Difference
    /// </summary>
    Task<ShiftDifferenceDto> CalculateDifferenceAsync(int shiftId, decimal actualClosingBalance, CancellationToken ct = default);

    /// <summary>
    /// UC130 - Add Notes (when closing with difference)
    /// </summary>
    Task<bool> AddClosingNotesAsync(ShiftClosingNotesRequestDto request, CancellationToken ct = default);

    /// <summary>
    /// UC131 - Confirm Closing
    /// </summary>
    Task<ShiftResponseDto> ConfirmClosingAsync(ShiftClosingConfirmRequestDto request, CancellationToken ct = default);

    // ========== HANDOVER ==========
    /// <summary>
    /// UC132 - Get available staff for handover
    /// </summary>
    Task<List<ShiftStaffDto>> GetAvailableHandoverStaffAsync(int currentStaffId, CancellationToken ct = default);

    /// <summary>
    /// UC133 - Save handover notes
    /// </summary>
    Task<bool> SaveHandoverNotesAsync(ShiftHandoverNotesRequestDto request, CancellationToken ct = default);

    /// <summary>
    /// UC134 - Verify PIN code for handover
    /// </summary>
    Task<bool> VerifyHandoverPinAsync(ShiftHandoverPinRequestDto request, CancellationToken ct = default);

    /// <summary>
    /// UC135 - Create next shift after handover
    /// </summary>
    Task<ShiftHandoverResponseDto> CreateNextShiftAfterHandoverAsync(ShiftHandoverCreateNextRequestDto request, CancellationToken ct = default);

    // ========== DASHBOARD & HISTORY ==========
    /// <summary>
    /// UC121 - View Shift Statistics (Dashboard)
    /// </summary>
    Task<ShiftDashboardDto> GetShiftDashboardAsync(int staffId, CancellationToken ct = default);

    /// <summary>
    /// UC122 - Get Opening Balance details
    /// </summary>
    Task<decimal?> GetOpeningBalanceAsync(int shiftId, CancellationToken ct = default);

    /// <summary>
    /// UC123 - Get Total Revenue for current shift
    /// </summary>
    Task<decimal> GetShiftRevenueAsync(int shiftId, CancellationToken ct = default);

    /// <summary>
    /// UC124 - Get Total Orders for current shift
    /// </summary>
    Task<int> GetShiftOrderCountAsync(int shiftId, CancellationToken ct = default);

    /// <summary>
    /// UC136 - Filter Shift History
    /// </summary>
    Task<ShiftHistoryListDto> GetShiftHistoryAsync(ShiftFilterDto filter, CancellationToken ct = default);

    /// <summary>
    /// UC137 - View Shift Details
    /// </summary>
    Task<ShiftDetailDto> GetShiftDetailsAsync(int shiftId, CancellationToken ct = default);

    /// <summary>
    /// UC138 - Export Shift Report (returns PDF bytes or file path)
    /// </summary>
    Task<byte[]> ExportShiftReportAsync(int shiftId, CancellationToken ct = default);

    // ========== UTILITY METHODS ==========
    /// <summary>
    /// Get current open shift for staff
    /// </summary>
    Task<ShiftDto?> GetCurrentOpenShiftAsync(int staffId, CancellationToken ct = default);

    /// <summary>
    /// Check if staff has open shift
    /// </summary>
    Task<bool> HasOpenShiftAsync(int staffId, CancellationToken ct = default);

    /// <summary>
    /// Get shift by ID
    /// </summary>
    Task<ShiftDto?> GetShiftByIdAsync(int shiftId, CancellationToken ct = default);
}
