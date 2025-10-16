using AutoMapper;
using BusinessAccessLayer.DTOs;
using BusinessAccessLayer.DTOs.UserManagement;
using DomainAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.Mapping
{
    public class MappingProfile : Profile
    {

        public MappingProfile()
        {

            CreateMap<MenuItem, ManagerMenuDTO>();
            CreateMap<MenuCategory, ManagerMenuCategoryDTO>();

            CreateMap<Combo, ManagerComboDTO>();
            CreateMap<ComboItem, ManagerComboItemDTO>().ForMember(dest => dest.ManagerMenuItem,
                      opt => opt.MapFrom(src => src.MenuItem)); ;

            // User -> StaffProfileDto
            CreateMap<User, StaffProfileDto>()
                .ForMember(d => d.RoleName, m => m.MapFrom(s => s.Role.RoleName))
                .ForMember(d => d.PositionNames, m => m.MapFrom(s => s.Staff.SelectMany(st => st.Positions.Select(p => p.PositionName)).Distinct().ToList()))
                .ForMember(d => d.DateOfBirth, m => m.Ignore())
                .ForMember(d => d.Gender, m => m.Ignore());
        }
    }
}
