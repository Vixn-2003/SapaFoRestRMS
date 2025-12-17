using AutoMapper;
using BusinessAccessLayer.DTOs.Positions;
using BusinessAccessLayer.DTOs.Staff;
using DomainAccessLayer.Models;
using System.Linq;

namespace BusinessAccessLayer.Mapping
{
    /// <summary>
    /// AutoMapper profile for Staff Management Module
    /// </summary>
    public class StaffManagementMappingProfile : Profile
    {
        public StaffManagementMappingProfile()
        {
            // Staff -> StaffListItemDto
            CreateMap<Staff, StaffListItemDto>()
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.User != null ? src.User.FullName : "N/A"))
                .ForMember(dest => dest.Phone, opt => opt.MapFrom(src => src.User != null ? src.User.Phone : null))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.User != null ? src.User.Email : ""))
                .ForMember(dest => dest.AvatarUrl, opt => opt.MapFrom(src => src.User != null ? src.User.AvatarUrl : null))
                .ForMember(dest => dest.Positions, opt => opt.MapFrom(src => string.Join(", ", src.Positions.Select(p => p.PositionName))))
                .ForMember(dest => dest.BaseSalary, opt => opt.MapFrom(src => src.SalaryBase))
                .ForMember(dest => dest.StatusText, opt => opt.MapFrom(src => src.Status == 0 ? "Active" : "Inactive")) // 0 = Active, 1 = Inactive
                .ForMember(dest => dest.DepartmentName, opt => opt.MapFrom(src => src.Department != null ? src.Department.Name : null));

            // Staff -> StaffDetailDto
            CreateMap<Staff, StaffDetailDto>()
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.User.FullName))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.User.Email))
                .ForMember(dest => dest.Phone, opt => opt.MapFrom(src => src.User.Phone))
                .ForMember(dest => dest.AvatarUrl, opt => opt.MapFrom(src => src.User.AvatarUrl))
                .ForMember(dest => dest.BaseSalary, opt => opt.MapFrom(src => src.SalaryBase))
                .ForMember(dest => dest.StatusText, opt => opt.MapFrom(src => src.Status == 0 ? "Active" : "Inactive")) // 0 = Active, 1 = Inactive
                .ForMember(dest => dest.DepartmentName, opt => opt.MapFrom(src => src.Department != null ? src.Department.Name : null))
                .ForMember(dest => dest.RoleId, opt => opt.MapFrom(src => src.User.RoleId))
                .ForMember(dest => dest.RoleName, opt => opt.MapFrom(src => src.User.Role != null ? src.User.Role.RoleName : "Unknown"))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.User.CreatedAt))
                .ForMember(dest => dest.ModifiedAt, opt => opt.MapFrom(src => src.User.ModifiedAt))
                .ForMember(dest => dest.Positions, opt => opt.MapFrom(src => src.Positions.Select(p => new StaffPositionDto
                {
                    PositionId = p.PositionId,
                    PositionName = p.PositionName
                }).ToList()));

            // Position -> StaffPositionDto
            CreateMap<Position, StaffPositionDto>()
                .ForMember(dest => dest.PositionName, opt => opt.MapFrom(src => src.PositionName));

            // Position -> PositionDto (for dropdown)
            CreateMap<Position, PositionDto>();

            // StaffCreateDto -> Staff (not used directly, but good to have)
            CreateMap<StaffCreateDto, Staff>()
                .ForMember(dest => dest.SalaryBase, opt => opt.MapFrom(src => src.BaseSalary))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => 0)) // Default to Active (0 = Active, 1 = Inactive)
                .ForMember(dest => dest.User, opt => opt.Ignore()) // Set manually
                .ForMember(dest => dest.Positions, opt => opt.Ignore()); // Set manually

            // StaffUpdateDto -> Staff (not used directly, but good to have)
            CreateMap<StaffUpdateDto, Staff>()
                .ForMember(dest => dest.SalaryBase, opt => opt.MapFrom(src => src.BaseSalary))
                .ForMember(dest => dest.User, opt => opt.Ignore()) // Set manually
                .ForMember(dest => dest.Positions, opt => opt.Ignore()); // Set manually
        }
    }
}

