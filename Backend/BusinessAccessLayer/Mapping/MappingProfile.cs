using AutoMapper;
using BusinessAccessLayer.DTOs.Inventory;
using BusinessAccessLayer.DTOs.Manager;
using BusinessAccessLayer.DTOs;
using BusinessAccessLayer.DTOs.UserManagement;
using BusinessAccessLayer.DTOs.Users;
using BusinessAccessLayer.DTOs.Positions;
using BusinessAccessLayer.DTOs.Payment;
using DomainAccessLayer.Models;
using Role = DomainAccessLayer.Models.Role;
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

            // Position mappings
            CreateMap<Position, PositionDto>();
            CreateMap<PositionCreateRequest, Position>();
            CreateMap<PositionUpdateRequest, Position>();

            // SalaryChangeRequest mappings
            CreateMap<SalaryChangeRequest, SalaryChangeRequestDto>()
                .ForMember(d => d.PositionName, m => m.MapFrom(s => s.Position != null ? s.Position.PositionName : ""))
                .ForMember(d => d.RequestedByName, m => m.MapFrom(s => s.RequestedByUser != null ? s.RequestedByUser.FullName : ""))
                .ForMember(d => d.ApprovedByName, m => m.MapFrom(s => s.ApprovedByUser != null ? s.ApprovedByUser.FullName : null));

            // Role mappings
            CreateMap<Role, RoleDto>()
                .ForMember(d => d.Description, m => m.MapFrom(s => string.Empty)); // Role model doesn't have Description

            // Payment mappings
            CreateMap<Order, OrderDto>()
                .ForMember(d => d.OrderCode, m => m.MapFrom(s => $"ORD-{s.OrderId:D6}"))
                .ForMember(d => d.Subtotal, m => m.Ignore())
                .ForMember(d => d.VatAmount, m => m.Ignore())
                .ForMember(d => d.ServiceFee, m => m.Ignore())
                .ForMember(d => d.DiscountAmount, m => m.Ignore())
                .ForMember(d => d.CustomerName, m => m.Ignore())
                .ForMember(d => d.TableNumber, m => m.Ignore())
                .ForMember(d => d.StaffName, m => m.Ignore())
                .ForMember(d => d.OrderItems, m => m.MapFrom(s => s.OrderDetails));

            CreateMap<OrderDetail, OrderItemDto>()
                .ForMember(d => d.MenuItemName, m => m.MapFrom(s => s.MenuItem != null ? s.MenuItem.Name : ""))
                .ForMember(d => d.TotalPrice, m => m.MapFrom(s => s.UnitPrice * s.Quantity));

            CreateMap<Transaction, TransactionDto>();
      }
    }
}
