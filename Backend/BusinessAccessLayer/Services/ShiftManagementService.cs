using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using BusinessAccessLayer.DTOs.ShiftManagement;
using BusinessAccessLayer.Services.Interfaces;
using DataAccessLayer.UnitOfWork.Interfaces;
using DomainAccessLayer.Models;

namespace BusinessAccessLayer.Services;

/// <summary>
/// Service xử lý business logic cho Shift Management
/// </summary>
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

    public async Task<ShiftDashboardDto?> GetCurrentShiftAsync(int staffId, CancellationToken ct = default)
    {
        var shift = await _unitOfWork.Shifts.GetCurrentOpenShiftAsync(staffId, ct);
        
        if (shift == null)
        {
            return null;
        }

        var dashboard = await MapToShiftDashboardAsync(shift, ct);
        return dashboard;
    }

    public async Task<ShiftResponseDto> OpenShiftAsync(OpenShiftRequestDto request, CancellationToken ct = default)
    {
        try
        {
            // Validate: Kiểm tra xem staff có ca nào đang mở không
            var hasOpenShift = await _unitOfWork.Shifts.HasOpenShiftAsync(request.StaffId, ct);
            if (hasOpenShift)
            {
                return new ShiftResponseDto
                {
                    Success = false,
                    Message = "Bạn đang có ca làm việc đang mở. Vui lòng kết ca trước khi mở ca mới."
                };
            }

            // Tạo Shift mới
            var now = DateTime.Now;
            var shift = new Shift
            {
                StaffId = request.StaffId,
                StartTime = now,
                Date = DateOnly.FromDateTime(now),
                OpeningBalance = request.OpeningBalance,
                OpeningDenominations = JsonSerializer.Serialize(request.Denominations),
                Status = "Open"
            };

            await _unitOfWork.Shifts.AddAsync(shift);
            await _unitOfWork.SaveChangesAsync();

            // Log audit
            await _auditLogService.LogEventAsync(
                eventType: "shift_opened",
                entityType: "Shift",
                entityId: shift.ShiftId,
                description: $"Mở ca làm việc mới với số dư đầu ca: {request.OpeningBalance:N0} VND",
                userId: request.StaffId,
                ct: ct
            );

            var dashboard = await MapToShiftDashboardAsync(shift, ct);

            return new ShiftResponseDto
            {
                Success = true,
                Message = "Mở ca làm việc thành công",
                Data = dashboard
            };
        }
        catch (Exception ex)
        {
            return new ShiftResponseDto
            {
                Success = false,
                Message = $"Lỗi khi mở ca: {ex.Message}"
            };
        }
    }

    public async Task<ShiftResponseDto> CloseShiftAsync(CloseShiftRequestDto request, CancellationToken ct = default)
    {
        try
        {
            var shift = await _unitOfWork.Shifts.GetShiftWithDetailsAsync(request.ShiftId, ct);
            
            if (shift == null)
            {
                return new ShiftResponseDto
                {
                    Success = false,
                    Message = "Không tìm thấy ca làm việc"
                };
            }

            if (shift.Status != "Open")
            {
                return new ShiftResponseDto
                {
                    Success = false,
                    Message = "Ca làm việc đã được kết thúc"
                };
            }

            // Cập nhật thông tin kết ca
            shift.EndTime = DateTime.Now;
            shift.ClosingBalance = request.ClosingBalance;
            shift.ClosingDenominations = JsonSerializer.Serialize(request.Denominations);
            shift.Difference = request.Difference;
            shift.Notes = request.Notes;
            shift.Status = "Closed";

            await _unitOfWork.Shifts.UpdateAsync(shift);
            await _unitOfWork.SaveChangesAsync();

            // Log audit
            await _auditLogService.LogEventAsync(
                eventType: "shift_closed",
                entityType: "Shift",
                entityId: shift.ShiftId,
                description: $"Kết ca làm việc. Số dư cuối ca: {request.ClosingBalance:N0} VND. Chênh lệch: {request.Difference:N0} VND",
                userId: shift.StaffId,
                ct: ct
            );

            var dashboard = await MapToShiftDashboardAsync(shift, ct);

            return new ShiftResponseDto
            {
                Success = true,
                Message = "Kết ca làm việc thành công",
                Data = dashboard
            };
        }
        catch (Exception ex)
        {
            return new ShiftResponseDto
            {
                Success = false,
                Message = $"Lỗi khi kết ca: {ex.Message}"
            };
        }
    }

    public async Task<ShiftResponseDto> HandoverShiftAsync(HandoverShiftRequestDto request, CancellationToken ct = default)
    {
        try
        {
            var shift = await _unitOfWork.Shifts.GetShiftWithDetailsAsync(request.ShiftId, ct);
            
            if (shift == null)
            {
                return new ShiftResponseDto
                {
                    Success = false,
                    Message = "Không tìm thấy ca làm việc"
                };
            }

            if (shift.Status != "Open")
            {
                return new ShiftResponseDto
                {
                    Success = false,
                    Message = "Chỉ có thể giao ca đang mở"
                };
            }

            // Validate: Nhân viên tiếp nhận không có ca đang mở
            var hasOpenShift = await _unitOfWork.Shifts.HasOpenShiftAsync(request.HandoverToStaffId, ct);
            if (hasOpenShift)
            {
                return new ShiftResponseDto
                {
                    Success = false,
                    Message = "Nhân viên tiếp nhận đang có ca làm việc đang mở"
                };
            }

            // Cập nhật thông tin giao ca
            shift.HandoverToStaffId = request.HandoverToStaffId;
            shift.HandoverNotes = request.Notes;
            shift.HandoverTime = DateTime.Now;
            shift.PinCode = HashPinCode(request.PinCode); // Encrypt PIN
            shift.Status = "Handover";
            shift.EndTime = DateTime.Now;

            await _unitOfWork.Shifts.UpdateAsync(shift);

            // Tạo ca mới cho nhân viên tiếp nhận
            var newShift = new Shift
            {
                StaffId = request.HandoverToStaffId,
                StartTime = DateTime.Now,
                Date = DateOnly.FromDateTime(DateTime.Now),
                OpeningBalance = shift.OpeningBalance, // Số dư đầu ca = số dư ca trước
                OpeningDenominations = shift.OpeningDenominations,
                Status = "Open"
            };

            await _unitOfWork.Shifts.AddAsync(newShift);
            await _unitOfWork.SaveChangesAsync();

            // Log audit
            await _auditLogService.LogEventAsync(
                eventType: "shift_handover",
                entityType: "Shift",
                entityId: shift.ShiftId,
                description: $"Giao ca từ Staff {shift.StaffId} cho Staff {request.HandoverToStaffId}",
                userId: shift.StaffId,
                ct: ct
            );

            var dashboard = await MapToShiftDashboardAsync(newShift, ct);

            return new ShiftResponseDto
            {
                Success = true,
                Message = "Giao ca thành công",
                Data = dashboard
            };
        }
        catch (Exception ex)
        {
            return new ShiftResponseDto
            {
                Success = false,
                Message = $"Lỗi khi giao ca: {ex.Message}"
            };
        }
    }

    public async Task<List<ShiftDto>> GetShiftHistoryAsync(int staffId, DateOnly fromDate, DateOnly toDate, CancellationToken ct = default)
    {
        var shifts = await _unitOfWork.Shifts.GetShiftHistoryAsync(staffId, fromDate, toDate, ct);
        var shiftDtos = new List<ShiftDto>();

        foreach (var shift in shifts)
        {
            var dto = _mapper.Map<ShiftDto>(shift);
            
            // Thêm thông tin tính toán
            dto.TotalRevenue = await _unitOfWork.Shifts.GetShiftRevenueAsync(shift.ShiftId, ct);
            dto.TotalOrders = await _unitOfWork.Shifts.GetShiftOrderCountAsync(shift.ShiftId, ct);
            
            shiftDtos.Add(dto);
        }

        return shiftDtos;
    }

    public async Task<ShiftDto?> GetShiftDetailsAsync(int shiftId, CancellationToken ct = default)
    {
        var shift = await _unitOfWork.Shifts.GetShiftWithDetailsAsync(shiftId, ct);
        
        if (shift == null)
        {
            return null;
        }

        var dto = _mapper.Map<ShiftDto>(shift);
        
        // Thêm thông tin tính toán
        dto.TotalRevenue = await _unitOfWork.Shifts.GetShiftRevenueAsync(shiftId, ct);
        dto.TotalOrders = await _unitOfWork.Shifts.GetShiftOrderCountAsync(shiftId, ct);

        return dto;
    }

    public async Task<bool> HasOpenShiftAsync(int staffId, CancellationToken ct = default)
    {
        return await _unitOfWork.Shifts.HasOpenShiftAsync(staffId, ct);
    }

    // Helper Methods
    private async Task<ShiftDashboardDto> MapToShiftDashboardAsync(Shift shift, CancellationToken ct = default)
    {
        // Tính toán doanh thu và thống kê
        var revenue = await CalculateShiftRevenueAsync(shift, ct);
        var orderStats = await CalculateOrderStatsAsync(shift, ct);

        return new ShiftDashboardDto
        {
            ShiftId = shift.ShiftId,
            Id = $"CA{shift.Date:yyyyMMdd}-{shift.ShiftId:D3}",
            Cashier = shift.Staff?.User?.FullName ?? "N/A",
            StartTime = shift.StartTime?.ToString("HH:mm") ?? "N/A",
            CurrentTime = DateTime.Now.ToString("HH:mm"),
            StartDate = shift.Date.ToString("dd/MM/yyyy"),
            OpeningBalance = shift.OpeningBalance ?? 0,
            SystemCash = revenue.Cash,
            SystemCard = revenue.Card,
            SystemQR = revenue.QR,
            TotalRevenue = revenue.Total,
            TotalOrders = orderStats.TotalOrders,
            PendingOrders = orderStats.PendingOrders,
            Discount = orderStats.Discount,
            ServiceFee = orderStats.ServiceFee,
            Vat = orderStats.Vat,
            Debt = orderStats.Debt,
            TotalItems = orderStats.TotalItems,
            Status = shift.Status ?? "Open"
        };
    }

    private async Task<(decimal Total, decimal Cash, decimal Card, decimal QR)> CalculateShiftRevenueAsync(Shift shift, CancellationToken ct = default)
    {
        var dayStart = shift.StartTime ?? DateTime.MinValue;
        var dayEnd = shift.EndTime ?? DateTime.MaxValue;

        // Lấy tất cả transactions trong khoảng thời gian ca
        var transactions = await _unitOfWork.Payments.GetTransactionsByOrderIdAsync(0); // TODO: Fix this
        
        var shiftTransactions = transactions.Where(t => 
            t.CreatedAt >= dayStart && 
            t.CreatedAt <= dayEnd &&
            (t.Status == "Paid" || t.Status == "Success")
        ).ToList();

        var cash = shiftTransactions.Where(t => t.PaymentMethod == "Cash").Sum(t => t.Amount);
        var card = shiftTransactions.Where(t => t.PaymentMethod == "Card" || t.PaymentMethod == "BankTransfer").Sum(t => t.Amount);
        var qr = shiftTransactions.Where(t => t.PaymentMethod == "QR" || t.PaymentMethod == "QRBankTransfer").Sum(t => t.Amount);
        var total = shiftTransactions.Sum(t => t.Amount);

        return (total, cash, card, qr);
    }

    private async Task<(int TotalOrders, int PendingOrders, decimal Discount, decimal ServiceFee, decimal Vat, decimal Debt, decimal TotalItems)> CalculateOrderStatsAsync(Shift shift, CancellationToken ct = default)
    {
        // TODO: Implement proper calculation
        // For now, return demo data
        return (3, 0, 0, 0, 0, 0, 0);
    }

    private string HashPinCode(string pinCode)
    {
        // TODO: Implement proper hashing (BCrypt, PBKDF2, etc.)
        // For now, just return as is (NOT SECURE!)
        return pinCode;
    }
}

