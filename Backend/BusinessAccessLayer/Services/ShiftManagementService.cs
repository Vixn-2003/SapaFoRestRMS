using AutoMapper;
using BusinessAccessLayer.DTOs.ShiftManagement;
using BusinessAccessLayer.Services.Interfaces;
using DataAccessLayer.UnitOfWork.Interfaces;
using DomainAccessLayer.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace BusinessAccessLayer.Services;

public class ShiftManagementService : IShiftManagementService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IAuditLogService _auditLogService;

    public ShiftManagementService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IAuditLogService auditLogService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _auditLogService = auditLogService;
    }

    // ========== OPENING SHIFT ==========

    /// <summary>
    /// UC125 - Declare Opening Balance
    /// </summary>
    public async Task<ShiftOpeningResponseDto> DeclareOpeningBalanceAsync(
        ShiftOpeningDeclareRequestDto request,
        CancellationToken ct = default)
    {
        // Business rule: Staff cannot open two shifts simultaneously
        var hasOpenShift = await _unitOfWork.ShiftCounters.HasOpenShiftAsync(request.StaffId, ct);
        if (hasOpenShift)
        {
            return new ShiftOpeningResponseDto
            {
                Success = false,
                Message = "Bạn đang có ca làm việc đang mở. Vui lòng kết ca trước khi mở ca mới."
            };
        }

        // Business rule: OpeningBalance > 0
        if (request.OpeningBalance <= 0)
        {
            return new ShiftOpeningResponseDto
            {
                Success = false,
                Message = "Số dư đầu ca phải lớn hơn 0."
            };
        }

        // Create new shift with status "PendingOpening" (temporary until confirmed)
        var shift = new Shift
        {
            StaffId = request.StaffId,
            Date = DateTime.UtcNow,
            StartTime = TimeSpan.FromHours(DateTime.UtcNow.Hour).Add(TimeSpan.FromMinutes(DateTime.UtcNow.Minute)),
            OpeningBalance = request.OpeningBalance,
            Status = "PendingOpening",
            Notes = request.Notes,
            Code = $"SHIFT-{DateTime.UtcNow:yyyyMMddHHmmss}",
            // Temporary values for required fields
            TemplateId = 1,
            DepartmentId = 1,
            EndTime = TimeSpan.FromHours(23).Add(TimeSpan.FromMinutes(59)),
            RequiredEmployees = 1
        };

        await _unitOfWork.ShiftCounters.AddAsync(shift);
        await _unitOfWork.SaveChangesAsync();

        // Add history
        await AddShiftHistoryAsync(shift.Id, request.StaffId, "DeclareOpening", $"Khai báo số dư đầu ca: {request.OpeningBalance:N0} VND", ct);
        await _unitOfWork.SaveChangesAsync();

        var shiftDto = _mapper.Map<ShiftDto>(shift);

        return new ShiftOpeningResponseDto
        {
            Success = true,
            Message = "Khai báo số dư đầu ca thành công.",
            Shift = shiftDto
        };
    }

    /// <summary>
    /// UC126 - Submit Opening Denominations
    /// </summary>
    public async Task<ShiftDenominationsResponseDto> SubmitOpeningDenominationsAsync(
        ShiftOpeningDenominationsRequestDto request,
        CancellationToken ct = default)
    {
        var shift = await _unitOfWork.ShiftCounters.GetByIdAsync(request.ShiftId);
        if (shift == null)
        {
            return new ShiftDenominationsResponseDto
            {
                Success = false,
                Message = "Không tìm thấy ca làm việc."
            };
        }

        // Calculate total from denominations
        var totalCalculated = request.Denominations.Sum(d => d.Total);

        // Business rule: Denominations must match opening balance
        if (Math.Abs(totalCalculated - (shift.OpeningBalance ?? 0)) > 0.01m)
        {
            return new ShiftDenominationsResponseDto
            {
                Success = false,
                Message = $"Tổng mệnh giá ({totalCalculated:N0} VND) không khớp với số dư đầu ca ({shift.OpeningBalance:N0} VND)."
            };
        }

        // Save denominations as JSON
        shift.OpeningDenominations = JsonSerializer.Serialize(request.Denominations);
        await _unitOfWork.ShiftCounters.UpdateAsync(shift);
        await _unitOfWork.SaveChangesAsync();

        // Add history
        await AddShiftHistoryAsync(shift.Id, shift.StaffId ?? 0, "SubmitOpeningDenominations", $"Nhập mệnh giá tiền đầu ca: {totalCalculated:N0} VND", ct);
        await _unitOfWork.SaveChangesAsync();

        return new ShiftDenominationsResponseDto
        {
            Success = true,
            Message = "Lưu mệnh giá tiền đầu ca thành công.",
            Denominations = request.Denominations,
            TotalAmount = totalCalculated
        };
    }

    /// <summary>
    /// UC127 - Confirm Shift Opening
    /// </summary>
    public async Task<ShiftOpeningResponseDto> ConfirmShiftOpeningAsync(
        ShiftOpeningConfirmRequestDto request,
        CancellationToken ct = default)
    {
        var shift = await _unitOfWork.ShiftCounters.GetByIdAsync(request.ShiftId);
        if (shift == null)
        {
            return new ShiftOpeningResponseDto
            {
                Success = false,
                Message = "Không tìm thấy ca làm việc."
            };
        }

        // Update shift status to "Open"
        shift.Status = "Open";
        shift.StartTime = TimeSpan.FromHours(DateTime.UtcNow.Hour).Add(TimeSpan.FromMinutes(DateTime.UtcNow.Minute));

        await _unitOfWork.ShiftCounters.UpdateAsync(shift);
        await _unitOfWork.SaveChangesAsync();

        // Add history
        await AddShiftHistoryAsync(shift.Id, request.StaffId, "ConfirmOpening", "Xác nhận mở ca làm việc", ct);
        await _unitOfWork.SaveChangesAsync();

        var shiftDto = _mapper.Map<ShiftDto>(shift);

        return new ShiftOpeningResponseDto
        {
            Success = true,
            Message = "Mở ca làm việc thành công!",
            Shift = shiftDto
        };
    }

    // ========== CLOSING SHIFT ==========

    /// <summary>
    /// UC128 - Count Closing Cash
    /// </summary>
    public async Task<ShiftDenominationsResponseDto> CountClosingCashAsync(
        ShiftDenominationsRequestDto request,
        CancellationToken ct = default)
    {
        var shift = await _unitOfWork.ShiftCounters.GetByIdAsync(request.ShiftId);
        if (shift == null)
        {
            return new ShiftDenominationsResponseDto
            {
                Success = false,
                Message = "Không tìm thấy ca làm việc."
            };
        }

        // Business rule: Staff must have an open shift
        if (shift.Status != "Open")
        {
            return new ShiftDenominationsResponseDto
            {
                Success = false,
                Message = "Ca làm việc không ở trạng thái mở."
            };
        }

        // Save closing denominations as JSON
        shift.ClosingDenominations = JsonSerializer.Serialize(request.Denominations);
        shift.ClosingBalance = request.TotalAmount;

        await _unitOfWork.ShiftCounters.UpdateAsync(shift);
        await _unitOfWork.SaveChangesAsync();

        // Add history
        await AddShiftHistoryAsync(shift.Id, shift.StaffId ?? 0, "CountClosingCash", $"Kiểm kê tiền cuối ca: {request.TotalAmount:N0} VND", ct);
        await _unitOfWork.SaveChangesAsync();

        return new ShiftDenominationsResponseDto
        {
            Success = true,
            Message = "Lưu thông tin kiểm kê tiền cuối ca thành công.",
            Denominations = request.Denominations.Select(d => new ShiftDenominationDto
            {
                Denomination = d.Denomination,
                Count = d.Count
            }).ToList(),
            TotalAmount = request.TotalAmount
        };
    }

    /// <summary>
    /// UC129 - Calculate Difference
    /// </summary>
    public async Task<ShiftDifferenceDto> CalculateDifferenceAsync(
        int shiftId,
        decimal actualClosingBalance,
        CancellationToken ct = default)
    {
        var shift = await _unitOfWork.ShiftCounters.GetByIdAsync(shiftId);
        if (shift == null)
        {
            throw new KeyNotFoundException("Không tìm thấy ca làm việc.");
        }

        var openingBalance = shift.OpeningBalance ?? 0;

        // Get revenue from cash payments only
        // TODO: Query payments by shift date range and filter by PaymentMethod = "Cash"
        var totalRevenueCash = await _unitOfWork.ShiftCounters.GetShiftRevenueAsync(shiftId, ct);

        var expectedClosingBalance = openingBalance + totalRevenueCash;
        var difference = actualClosingBalance - expectedClosingBalance;

        // Save difference to shift
        shift.Difference = difference;
        await _unitOfWork.ShiftCounters.UpdateAsync(shift);
        await _unitOfWork.SaveChangesAsync();

        return new ShiftDifferenceDto
        {
            OpeningBalance = openingBalance,
            TotalRevenueCash = totalRevenueCash,
            ExpectedClosingBalance = expectedClosingBalance,
            ActualClosingBalance = actualClosingBalance,
            Difference = difference
        };
    }

    /// <summary>
    /// UC130 - Add Notes
    /// </summary>
    public async Task<bool> AddClosingNotesAsync(
        ShiftClosingNotesRequestDto request,
        CancellationToken ct = default)
    {
        var shift = await _unitOfWork.ShiftCounters.GetByIdAsync(request.ShiftId);
        if (shift == null) return false;

        shift.Notes = request.Notes;
        await _unitOfWork.ShiftCounters.UpdateAsync(shift);
        await _unitOfWork.SaveChangesAsync();

        // Add history
        await AddShiftHistoryAsync(shift.Id, shift.StaffId ?? 0, "AddClosingNotes", $"Ghi chú kết ca: {request.Notes}", ct);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    /// <summary>
    /// UC131 - Confirm Closing
    /// </summary>
    public async Task<ShiftResponseDto> ConfirmClosingAsync(
        ShiftClosingConfirmRequestDto request,
        CancellationToken ct = default)
    {
        var shift = await _unitOfWork.ShiftCounters.GetByIdAsync(request.ShiftId);
        if (shift == null)
        {
            return new ShiftResponseDto
            {
                Success = false,
                Message = "Không tìm thấy ca làm việc."
            };
        }

        // Business rule: Staff cannot close a shift they did not open
        if (shift.StaffId != request.StaffId)
        {
            return new ShiftResponseDto
            {
                Success = false,
                Message = "Bạn không thể kết ca của người khác."
            };
        }

        // Business rule: If |difference| > 0, require notes
        if (Math.Abs(shift.Difference ?? 0) > 0 && string.IsNullOrWhiteSpace(request.Notes))
        {
            return new ShiftResponseDto
            {
                Success = false,
                Message = "Vui lòng nhập ghi chú giải trình chênh lệch tiền mặt."
            };
        }

        // Update shift status
        shift.Status = "Closed";
        shift.EndTime = TimeSpan.FromHours(DateTime.UtcNow.Hour).Add(TimeSpan.FromMinutes(DateTime.UtcNow.Minute));
        shift.ClosingBalance = request.ClosingBalance;
        if (!string.IsNullOrWhiteSpace(request.Notes))
        {
            shift.Notes = request.Notes;
        }

        await _unitOfWork.ShiftCounters.UpdateAsync(shift);
        await _unitOfWork.SaveChangesAsync();

        // Add history
        await AddShiftHistoryAsync(shift.Id, request.StaffId, "ConfirmClosing", "Xác nhận kết ca", ct);
        await _unitOfWork.SaveChangesAsync();

        var dashboard = await GetShiftDashboardAsync(request.StaffId, ct);

        return new ShiftResponseDto
        {
            Success = true,
            Message = "Kết ca thành công!",
            Data = dashboard
        };
    }

    // ========== HANDOVER ==========

    /// <summary>
    /// UC132 - Get available staff for handover
    /// </summary>
    public async Task<List<ShiftStaffDto>> GetAvailableHandoverStaffAsync(
        int currentStaffId,
        CancellationToken ct = default)
    {
        // Get all users with staff details
        var allUsers = await _unitOfWork.StaffProfiles.GetAllWithDetailsAsync(ct);
        
        var result = new List<ShiftStaffDto>();

        foreach (var user in allUsers)
        {
            // Get the first staff record for this user (normally 1-to-1 relationship)
            var staff = user.Staff?.FirstOrDefault();
            
            if (staff == null || staff.StaffId == currentStaffId)
                continue;

            // Check if this staff has an open shift
            var hasOpenShift = await _unitOfWork.ShiftCounters.HasOpenShiftAsync(staff.StaffId, ct);

            // Get position name (first position if multiple)
            var positionName = staff.Positions?.FirstOrDefault()?.PositionName ?? "Counter Staff";

            result.Add(new ShiftStaffDto
            {
                StaffId = staff.StaffId,
                StaffName = user.FullName ?? "Unknown",
                Position = positionName,
                IsAvailable = !hasOpenShift,
                CurrentShiftStatus = hasOpenShift ? "Open" : null
            });
        }

        return result.OrderByDescending(s => s.IsAvailable).ToList();
    }

    /// <summary>
    /// UC133 - Save handover notes
    /// </summary>
    public async Task<bool> SaveHandoverNotesAsync(
        ShiftHandoverNotesRequestDto request,
        CancellationToken ct = default)
    {
        var shift = await _unitOfWork.ShiftCounters.GetByIdAsync(request.ShiftId);
        if (shift == null) return false;

        shift.HandoverNotes = request.HandoverNotes;
        await _unitOfWork.ShiftCounters.UpdateAsync(shift);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    /// <summary>
    /// UC134 - Verify PIN code
    /// </summary>
    public async Task<bool> VerifyHandoverPinAsync(
        ShiftHandoverPinRequestDto request,
        CancellationToken ct = default)
    {
        // Get all users and find the one with this staffId
        var allUsers = await _unitOfWork.StaffProfiles.GetAllWithDetailsAsync(ct);
        var user = allUsers.FirstOrDefault(u => 
            u.Staff != null && 
            u.Staff.Any(s => s.StaffId == request.FromStaffId));
        
        if (user == null) return false;

        // TODO: Implement proper PIN verification
        // For now, simple comparison (in production, should hash and compare)
        // Assuming PIN is stored in User.Password or a separate PIN field
        // You can compare: user.Password == HashPin(request.PinCode)

        return true; // Placeholder - implement actual verification
    }

    /// <summary>
    /// UC135 - Create next shift after handover
    /// </summary>
    public async Task<ShiftHandoverResponseDto> CreateNextShiftAfterHandoverAsync(
        ShiftHandoverCreateNextRequestDto request,
        CancellationToken ct = default)
    {
        var currentShift = await _unitOfWork.ShiftCounters.GetByIdAsync(request.CurrentShiftId);
        if (currentShift == null)
        {
            return new ShiftHandoverResponseDto
            {
                Success = false,
                Message = "Không tìm thấy ca làm việc hiện tại."
            };
        }

        // Verify PIN
        var isPinValid = await VerifyHandoverPinAsync(new ShiftHandoverPinRequestDto
        {
            ShiftId = request.CurrentShiftId,
            FromStaffId = request.FromStaffId,
            PinCode = request.PinCode
        }, ct);

        if (!isPinValid)
        {
            return new ShiftHandoverResponseDto
            {
                Success = false,
                Message = "Mã PIN không chính xác."
            };
        }

        // Close current shift
        currentShift.Status = "Handover";
        currentShift.EndTime = TimeSpan.FromHours(DateTime.UtcNow.Hour).Add(TimeSpan.FromMinutes(DateTime.UtcNow.Minute));
        currentShift.HandoverToStaffId = request.ToStaffId;
        currentShift.HandoverNotes = request.HandoverNotes;
        currentShift.HandoverTime = DateTime.UtcNow;
        currentShift.ClosingBalance = request.ClosingBalance;

        await _unitOfWork.ShiftCounters.UpdateAsync(currentShift);

        // Create new shift for next staff
        var nextShift = new Shift
        {
            StaffId = request.ToStaffId,
            Date = DateTime.UtcNow,
            StartTime = TimeSpan.FromHours(DateTime.UtcNow.Hour).Add(TimeSpan.FromMinutes(DateTime.UtcNow.Minute)),
            OpeningBalance = request.ClosingBalance, // Opening balance = closing balance of previous shift
            Status = "PendingOpening",
            Notes = $"Nhận ca từ staff {request.FromStaffId}",
            Code = $"SHIFT-{DateTime.UtcNow:yyyyMMddHHmmss}",
            TemplateId = 1,
            DepartmentId = 1,
            EndTime = TimeSpan.FromHours(23).Add(TimeSpan.FromMinutes(59)),
            RequiredEmployees = 1
        };

        await _unitOfWork.ShiftCounters.AddAsync(nextShift);
        await _unitOfWork.SaveChangesAsync();

        // Add history for both shifts
        await AddShiftHistoryAsync(currentShift.Id, request.FromStaffId, "Handover", $"Giao ca cho Staff ID {request.ToStaffId}", ct);
        await AddShiftHistoryAsync(nextShift.Id, request.ToStaffId, "ReceiveHandover", $"Nhận ca từ Staff ID {request.FromStaffId}", ct);
        await _unitOfWork.SaveChangesAsync();

        var currentShiftDto = _mapper.Map<ShiftDto>(currentShift);
        var nextShiftDto = _mapper.Map<ShiftDto>(nextShift);

        return new ShiftHandoverResponseDto
        {
            Success = true,
            Message = "Giao ca thành công!",
            CurrentShift = currentShiftDto,
            NextShift = nextShiftDto
        };
    }

    // ========== DASHBOARD & HISTORY ==========

    /// <summary>
    /// UC121 - View Shift Statistics
    /// </summary>
    public async Task<ShiftDashboardDto> GetShiftDashboardAsync(
        int staffId,
        CancellationToken ct = default)
    {
        var shift = await _unitOfWork.ShiftCounters.GetCurrentOpenShiftAsync(staffId, ct);

        if (shift == null)
        {
            return new ShiftDashboardDto
            {
                Status = "NoShift",
                Cashier = "Unknown"
            };
        }

        var revenue = await _unitOfWork.ShiftCounters.GetShiftRevenueAsync(shift.Id, ct);
        var orderCount = await _unitOfWork.ShiftCounters.GetShiftOrderCountAsync(shift.Id, ct);

        return new ShiftDashboardDto
        {
            ShiftId = shift.Id,
            Id = shift.Code,
            Cashier = shift.Staff?.User?.FullName ?? "Unknown",
            StartTime = shift.StartTime.ToString(@"hh\:mm"),
            CurrentTime = DateTime.Now.ToString("HH:mm"),
            StartDate = shift.Date.ToString("dd/MM/yyyy"),
            OpeningBalance = shift.OpeningBalance ?? 0,
            SystemCash = revenue, // Placeholder
            SystemCard = 0,
            SystemQR = 0,
            TotalRevenue = revenue,
            TotalOrders = orderCount,
            PendingOrders = 0, // TODO: Query pending orders
            Discount = 0,
            ServiceFee = 0,
            Vat = 0,
            Debt = 0,
            TotalItems = 0,
            Status = shift.Status ?? "Open"
        };
    }

    /// <summary>
    /// UC122 - Get Opening Balance
    /// </summary>
    public async Task<decimal?> GetOpeningBalanceAsync(int shiftId, CancellationToken ct = default)
    {
        var shift = await _unitOfWork.ShiftCounters.GetByIdAsync(shiftId);
        return shift?.OpeningBalance;
    }

    /// <summary>
    /// UC123 - Get Total Revenue
    /// </summary>
    public async Task<decimal> GetShiftRevenueAsync(int shiftId, CancellationToken ct = default)
    {
        return await _unitOfWork.ShiftCounters.GetShiftRevenueAsync(shiftId, ct);
    }

    /// <summary>
    /// UC124 - Get Total Orders
    /// </summary>
    public async Task<int> GetShiftOrderCountAsync(int shiftId, CancellationToken ct = default)
    {
        return await _unitOfWork.ShiftCounters.GetShiftOrderCountAsync(shiftId, ct);
    }

    /// <summary>
    /// UC136 - Filter Shift History
    /// </summary>
    public async Task<ShiftHistoryListDto> GetShiftHistoryAsync(
        ShiftFilterDto filter,
        CancellationToken ct = default)
    {
        var fromDate = filter.FromDate.HasValue ? DateOnly.FromDateTime(filter.FromDate.Value) : DateOnly.MinValue;
        var toDate = filter.ToDate.HasValue ? DateOnly.FromDateTime(filter.ToDate.Value) : DateOnly.MaxValue;

        var shifts = await _unitOfWork.ShiftCounters.GetShiftHistoryAsync(
            filter.StaffId ?? 0,
            fromDate,
            toDate,
            ct);

        var shiftList = shifts.ToList();

        // Apply status filter
        if (!string.IsNullOrWhiteSpace(filter.Status))
        {
            shiftList = shiftList.Where(s => s.Status == filter.Status).ToList();
        }

        var totalCount = shiftList.Count;
        var pagedShifts = shiftList
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToList();

        var shiftDtos = _mapper.Map<List<ShiftDto>>(pagedShifts);

        return new ShiftHistoryListDto
        {
            Shifts = shiftDtos,
            TotalCount = totalCount,
            PageNumber = filter.PageNumber,
            PageSize = filter.PageSize
        };
    }

    /// <summary>
    /// UC137 - View Shift Details
    /// </summary>
    public async Task<ShiftDetailDto> GetShiftDetailsAsync(int shiftId, CancellationToken ct = default)
    {
        var shift = await _unitOfWork.ShiftCounters.GetShiftWithDetailsAsync(shiftId, ct);
        if (shift == null)
        {
            throw new KeyNotFoundException("Không tìm thấy ca làm việc.");
        }

        var revenue = await _unitOfWork.ShiftCounters.GetShiftRevenueAsync(shiftId, ct);
        var orderCount = await _unitOfWork.ShiftCounters.GetShiftOrderCountAsync(shiftId, ct);
        var histories = await _unitOfWork.ShiftCounters.GetShiftHistoriesAsync(shiftId, ct);

        var detailDto = new ShiftDetailDto
        {
            ShiftId = shift.Id,
            ShiftCode = shift.Code,
            StaffId = shift.StaffId ?? 0,
            StaffName = shift.Staff?.User?.FullName ?? "Unknown",
            Date = shift.Date,
            StartTime = DateTime.Today.Add(shift.StartTime),
            EndTime = DateTime.Today.Add(shift.EndTime),
            Status = shift.Status ?? "Unknown",
            OpeningBalance = shift.OpeningBalance,
            ClosingBalance = shift.ClosingBalance,
            Difference = shift.Difference,
            Notes = shift.Notes,
            OpeningDenominations = shift.OpeningDenominations,
            ClosingDenominations = shift.ClosingDenominations,
            TotalRevenue = revenue,
            TotalCash = revenue, // TODO: Filter by payment method
            TotalCard = 0,
            TotalQR = 0,
            TotalOrders = orderCount,
            HandoverToStaffId = shift.HandoverToStaffId,
            HandoverToStaffName = null, // TODO: Lookup handover staff name
            HandoverNotes = shift.HandoverNotes,
            HandoverTime = shift.HandoverTime,
            Histories = _mapper.Map<List<ShiftHistoryDto>>(histories)
        };

        return detailDto;
    }

    /// <summary>
    /// UC138 - Export Shift Report
    /// </summary>
    public async Task<byte[]> ExportShiftReportAsync(int shiftId, CancellationToken ct = default)
    {
        // TODO: Implement PDF generation using QuestPDF
        // For now, return empty byte array
        await Task.CompletedTask;
        return Array.Empty<byte>();
    }

    // ========== UTILITY METHODS ==========

    public async Task<ShiftDto?> GetCurrentOpenShiftAsync(int staffId, CancellationToken ct = default)
    {
        var shift = await _unitOfWork.ShiftCounters.GetCurrentOpenShiftAsync(staffId, ct);
        return shift != null ? _mapper.Map<ShiftDto>(shift) : null;
    }

    public async Task<bool> HasOpenShiftAsync(int staffId, CancellationToken ct = default)
    {
        return await _unitOfWork.ShiftCounters.HasOpenShiftAsync(staffId, ct);
    }

    public async Task<ShiftDto?> GetShiftByIdAsync(int shiftId, CancellationToken ct = default)
    {
        var shift = await _unitOfWork.ShiftCounters.GetByIdAsync(shiftId);
        return shift != null ? _mapper.Map<ShiftDto>(shift) : null;
    }

    // ========== PRIVATE HELPER METHODS ==========

    private async Task AddShiftHistoryAsync(
        int shiftId,
        int actionBy,
        string action,
        string detail,
        CancellationToken ct = default)
    {
        var history = new ShiftHistory
        {
            ShiftId = shiftId,
            ActionBy = actionBy,
            Action = action,
            ActionAt = DateTime.UtcNow,
            Detail = detail
        };

        await _unitOfWork.ShiftCounters.AddHistoryAsync(history, ct);
    }
}
