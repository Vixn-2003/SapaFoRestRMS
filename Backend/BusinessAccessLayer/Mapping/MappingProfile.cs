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
            CreateMap<ManagerMenuDTO, MenuItem>();

            CreateMap<Combo, ManagerComboDTO>();
            CreateMap<ComboItem, ManagerComboItemDTO>().ForMember(dest => dest.ManagerMenuItem,
                      opt => opt.MapFrom(src => src.MenuItem)); ;

            CreateMap<MenuCategory, ManagerCategoryDTO>();
            CreateMap<ManagerCategoryDTO, MenuCategory>();
            CreateMap<Recipe, RecipeDTO>();
            CreateMap<RecipeDTO, Recipe>();

            CreateMap<Ingredient, InventoryIngredientDTO>()
             .ForMember(dest => dest.Batches,
               opt => opt.MapFrom(src => src.InventoryBatches));

            CreateMap<InventoryBatch, InventoryBatchDTO>();
            CreateMap<InventoryBatchDTO, InventoryBatch>();

            CreateMap<StockTransaction, StockTransactionDTO>();

            // User -> StaffProfileDto
            CreateMap<User, StaffProfileDto>()
                .ForMember(d => d.RoleName, m => m.MapFrom(s => s.Role.RoleName))
                .ForMember(d => d.PositionNames, m => m.MapFrom(s => s.Staff.SelectMany(st => st.Positions.Select(p => p.PositionName)).Distinct().ToList()))
                .ForMember(d => d.DateOfBirth, m => m.Ignore())
                .ForMember(d => d.Gender, m => m.Ignore());

            CreateMap<Supplier, SupplierDTO>();
            CreateMap<SupplierDTO, Supplier>();
            CreateMap<PurchaseOrder, PurchaseOrderDTO>();
            CreateMap<PurchaseOrderDTO, PurchaseOrder>();
            CreateMap<PurchaseOrderDetailDTO, PurchaseOrderDetail>();
            CreateMap<PurchaseOrderDetail, PurchaseOrderDetailDTO>();
            CreateMap<Ingredient, IngredientDTO>();
            CreateMap<IngredientDTO, Ingredient>();

            //BatchIngredient 
            CreateMap<InventoryBatch, BatchIngredientDTO>()
                .ForMember(dest => dest.IngredientName, opt => opt.MapFrom(src => src.Ingredient.Name))
                .ForMember(dest => dest.IngredientUnit, opt => opt.MapFrom(src => src.Ingredient.Unit))
                .ForMember(dest => dest.WarehouseName, opt => opt.MapFrom(src => src.Warehouse.Name))
                .ForMember(dest => dest.PurchaseOrderId, opt => opt.MapFrom(src => src.PurchaseOrderDetail.PurchaseOrderId))
                .ForMember(dest => dest.OrderDate, opt => opt.MapFrom(src => src.PurchaseOrderDetail.PurchaseOrder.OrderDate))
                .ForMember(dest => dest.OrderStatus, opt => opt.MapFrom(src => src.PurchaseOrderDetail.PurchaseOrder.Status))
                .ForMember(dest => dest.SupplierId, opt => opt.MapFrom(src => src.PurchaseOrderDetail.PurchaseOrder.Supplier.SupplierId))
                .ForMember(dest => dest.SupplierName, opt => opt.MapFrom(src => src.PurchaseOrderDetail.PurchaseOrder.Supplier.Name))
                .ForMember(dest => dest.SupplierCode, opt => opt.MapFrom(src => src.PurchaseOrderDetail.PurchaseOrder.Supplier.CodeSupplier))
                .ForMember(dest => dest.SupplierPhone, opt => opt.MapFrom(src => src.PurchaseOrderDetail.PurchaseOrder.Supplier.Phone));
            CreateMap<WarehouseDTO, Warehouse>();
            CreateMap<Warehouse, WarehouseDTO>();
            CreateMap<StockTransaction, StockTransactionDTO>();
            CreateMap<StockTransactionDTO, StockTransaction>();
        


            // User mappings
            CreateMap<User, UserDto>()
                .ForMember(d => d.RoleName, m => m.Ignore()); // RoleName is loaded separately in service

            // Position mappings
            CreateMap<Position, PositionDto>();
            CreateMap<PositionCreateRequest, Position>();
            CreateMap<PositionUpdateRequest, Position>();

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
            CreateMap<Unit, UnitDTO>();
      }

    }
}
