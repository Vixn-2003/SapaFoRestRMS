using AutoMapper;
using BusinessAccessLayer.DTOs.ShiftManagement;
using DomainAccessLayer.Models;
using System;

namespace BusinessAccessLayer.Mapping;

public class ShiftManagementMappingProfile : Profile
{
    public ShiftManagementMappingProfile()
    {
        // Shift <-> ShiftDto
        CreateMap<Shift, ShiftDto>()
            .ForMember(dest => dest.ShiftId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.StaffId, opt => opt.MapFrom(src => src.StaffId ?? 0))
            .ForMember(dest => dest.StaffName, opt => opt.MapFrom(src => src.Staff != null && src.Staff.User != null ? src.Staff.User.FullName : "Unknown"))
            .ForMember(dest => dest.StartTime, opt => opt.MapFrom(src => DateTime.Today.Add(src.StartTime)))
            .ForMember(dest => dest.EndTime, opt => opt.MapFrom(src => DateTime.Today.Add(src.EndTime)))
            .ForMember(dest => dest.Date, opt => opt.MapFrom(src => DateOnly.FromDateTime(src.Date)))
            .ForMember(dest => dest.HandoverToStaffName, opt => opt.Ignore()) // TODO: Map from navigation property
            .ForMember(dest => dest.TotalRevenue, opt => opt.Ignore())
            .ForMember(dest => dest.SystemCash, opt => opt.Ignore())
            .ForMember(dest => dest.SystemCard, opt => opt.Ignore())
            .ForMember(dest => dest.SystemQR, opt => opt.Ignore())
            .ForMember(dest => dest.TotalOrders, opt => opt.Ignore())
            .ForMember(dest => dest.PendingOrders, opt => opt.Ignore())
            .ForMember(dest => dest.Discount, opt => opt.Ignore())
            .ForMember(dest => dest.ServiceFee, opt => opt.Ignore())
            .ForMember(dest => dest.Vat, opt => opt.Ignore())
            .ForMember(dest => dest.Debt, opt => opt.Ignore())
            .ForMember(dest => dest.TotalItems, opt => opt.Ignore());

        CreateMap<ShiftDto, Shift>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.ShiftId))
            .ForMember(dest => dest.Date, opt => opt.MapFrom(src => src.Date.ToDateTime(TimeOnly.MinValue)))
            .ForMember(dest => dest.StartTime, opt => opt.MapFrom(src => src.StartTime.HasValue ? src.StartTime.Value.TimeOfDay : TimeSpan.Zero))
            .ForMember(dest => dest.EndTime, opt => opt.MapFrom(src => src.EndTime.HasValue ? src.EndTime.Value.TimeOfDay : TimeSpan.Zero))
            .ForMember(dest => dest.Staff, opt => opt.Ignore())
            .ForMember(dest => dest.Template, opt => opt.Ignore())
            .ForMember(dest => dest.Department, opt => opt.Ignore())
            .ForMember(dest => dest.ShiftAssignments, opt => opt.Ignore());

        // ShiftHistory <-> ShiftHistoryDto
        CreateMap<ShiftHistory, ShiftHistoryDto>()
            .ForMember(dest => dest.ShiftCode, opt => opt.MapFrom(src => src.Shift != null ? src.Shift.Code : "Unknown"))
            .ForMember(dest => dest.ActionByName, opt => opt.Ignore()); // TODO: Map from User

        CreateMap<ShiftHistoryDto, ShiftHistory>()
            .ForMember(dest => dest.Shift, opt => opt.Ignore());

        // ShiftDetailDto mappings (manual in service)

        // Staff <-> ShiftStaffDto
        CreateMap<Staff, ShiftStaffDto>()
            .ForMember(dest => dest.StaffName, opt => opt.MapFrom(src => src.User != null ? src.User.FullName : "Unknown"))
            .ForMember(dest => dest.Position, opt => opt.MapFrom(src => 
                src.Positions != null && src.Positions.Any() 
                    ? src.Positions.First().PositionName 
                    : "Counter Staff"))
            .ForMember(dest => dest.IsAvailable, opt => opt.Ignore())
            .ForMember(dest => dest.CurrentShiftStatus, opt => opt.Ignore());

        // Additional mappings for OpenShiftRequestDto -> Shift (if needed)
        CreateMap<OpenShiftRequestDto, Shift>()
            .ForMember(dest => dest.OpeningBalance, opt => opt.MapFrom(src => src.OpeningBalance))
            .ForMember(dest => dest.StaffId, opt => opt.MapFrom(src => src.StaffId))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => "Open"))
            .ForMember(dest => dest.Date, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.StartTime, opt => opt.MapFrom(src => DateTime.UtcNow.TimeOfDay))
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Code, opt => opt.Ignore())
            .ForMember(dest => dest.TemplateId, opt => opt.Ignore())
            .ForMember(dest => dest.DepartmentId, opt => opt.Ignore())
            .ForMember(dest => dest.EndTime, opt => opt.Ignore())
            .ForMember(dest => dest.RequiredEmployees, opt => opt.Ignore())
            .ForMember(dest => dest.ClosingBalance, opt => opt.Ignore())
            .ForMember(dest => dest.OpeningDenominations, opt => opt.Ignore())
            .ForMember(dest => dest.ClosingDenominations, opt => opt.Ignore())
            .ForMember(dest => dest.Difference, opt => opt.Ignore())
            .ForMember(dest => dest.Notes, opt => opt.Ignore())
            .ForMember(dest => dest.HandoverToStaffId, opt => opt.Ignore())
            .ForMember(dest => dest.HandoverNotes, opt => opt.Ignore())
            .ForMember(dest => dest.HandoverTime, opt => opt.Ignore())
            .ForMember(dest => dest.PinCode, opt => opt.Ignore())
            .ForMember(dest => dest.Staff, opt => opt.Ignore())
            .ForMember(dest => dest.Template, opt => opt.Ignore())
            .ForMember(dest => dest.Department, opt => opt.Ignore())
            .ForMember(dest => dest.ShiftAssignments, opt => opt.Ignore());
    }
}

