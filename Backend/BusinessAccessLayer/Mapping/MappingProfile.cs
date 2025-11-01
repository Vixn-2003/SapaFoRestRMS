using AutoMapper;
using BusinessAccessLayer.DTOs.Inventory;
using BusinessAccessLayer.DTOs.Manager;
using BusinessAccessLayer.DTOs;
using BusinessAccessLayer.DTOs.UserManagement;
using BusinessAccessLayer.DTOs.Users;
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

            CreateMap<Combo, ManagerComboDTO>();
            CreateMap<ComboItem, ManagerComboItemDTO>().ForMember(dest => dest.ManagerMenuItem,
                      opt => opt.MapFrom(src => src.MenuItem)); ;

            CreateMap<MenuCategory, ManagerCategoryDTO>();
            CreateMap<ManagerCategoryDTO, MenuCategory>();

            CreateMap<InventoryBatch, InventoryIngredientWithBatchDTO>()
                .ForMember(dest => dest.IngredientId, opt => opt.MapFrom(src => src.Ingredient.IngredientId))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Ingredient.Name))
                .ForMember(dest => dest.Unit, opt => opt.MapFrom(src => src.Ingredient.Unit))
                .ForMember(dest => dest.ReorderLevel, opt => opt.MapFrom(src => src.Ingredient.ReorderLevel));

            // User -> StaffProfileDto
            CreateMap<User, StaffProfileDto>()
                .ForMember(d => d.RoleName, m => m.MapFrom(s => s.Role.RoleName))
                .ForMember(d => d.PositionNames, m => m.MapFrom(s => s.Staff.SelectMany(st => st.Positions.Select(p => p.PositionName)).Distinct().ToList()))
                .ForMember(d => d.DateOfBirth, m => m.Ignore())
                .ForMember(d => d.Gender, m => m.Ignore());

            // User mappings
            CreateMap<User, UserDto>()
                .ForMember(d => d.RoleName, m => m.Ignore()); // RoleName is loaded separately in service
      }
    }
}
